using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Flee,
        ReturnToPatrol
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

    [Header("Patrol Line")]
    [SerializeField] private LineRenderer patrolLine;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float patrolPointReachDistance = 0.5f;
    [SerializeField] private bool loopPatrol = true;

    [Header("Chase")]
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float chaseSpeed = 3.3f;
    [SerializeField] private ChaseDangerEffectController dangerEffectController;

    [Header("Flee")]
    [SerializeField] private FlashlightRepelRaycaster flashlightRepelRaycaster;
    [SerializeField] private EnemyFaceHitBox faceHitBox;
    [SerializeField] private float fleeSpeed = 4.0f;
    [SerializeField] private float fleeDuration = 5f;
    [SerializeField] private float fleeDistance = 6f;
    [SerializeField] private float navMeshSampleRadius = 3f;

    [Header("Death")]
    [SerializeField] private PlayerDeathHandler playerDeathHandler;
    [SerializeField] private string playerTag = "Player";

    [Header("Audio")]
    [SerializeField] private AudioManager audioManager;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationFadeTime = 0.15f;
    [SerializeField] private int baseLayerIndex = 0;
    [SerializeField] private int upperLayerIndex = 1;
    [SerializeField] private bool useUpperLayerForFlee = true;
    [SerializeField] private string patrolAnimationState = "Patrol";
    [SerializeField] private string chaseAnimationState = "Chase";
    [SerializeField] private string fleeAnimationState = "Flee";
    [SerializeField] private string returnToPatrolAnimationState = "ReturnToPatrol";
    [SerializeField] private string upperEmptyAnimationState = "UpperEmpty";
    [SerializeField] private string upperFleeAnimationState = "Flee";

    [Header("Debug")]
    [SerializeField] private bool drawDebugRange = true;
    [SerializeField] private bool logStateChanges = true;

    private EnemyState currentState;
    private Vector3[] patrolPositions = new Vector3[0];
    private int currentPatrolIndex;
    private float fleeTimer;
    private bool hasTouchedPlayer;
    private bool hasEnteredInitialState;
    private bool hasLoggedAgentNotOnNavMesh;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ValidateReferences();
    }

    private void Start()
    {
        CachePatrolPositions();
        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
        {
            if (!hasLoggedAgentNotOnNavMesh)
            {
                Debug.LogWarning("EnemyAI: NavMeshAgent is not on a NavMesh.");
                hasLoggedAgentNotOnNavMesh = true;
            }

            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Flee:
                UpdateFlee();
                break;
            case EnemyState.ReturnToPatrol:
                UpdateReturnToPatrol();
                break;
        }
    }

    public void RefreshPatrolPath()
    {
        CachePatrolPositions();
    }

    public void RequestFleeFromLight()
    {
        if (currentState == EnemyState.Flee)
            return;

        ChangeState(EnemyState.Flee);
    }

    private void ChangeState(EnemyState nextState)
    {
        if (hasEnteredInitialState && currentState == nextState)
            return;

        EnemyState previousState = currentState;
        if (hasEnteredInitialState)
            ExitState(previousState);

        currentState = nextState;
        hasEnteredInitialState = true;
        EnterState(nextState);

        if (logStateChanges)
            Debug.Log($"EnemyAI: {previousState} -> {nextState}");
    }

    private void EnterState(EnemyState state)
    {
        PlayStateAnimation(state);

        switch (state)
        {
            case EnemyState.Patrol:
                if (agent != null)
                    agent.speed = patrolSpeed;
                SetCurrentPatrolDestination();
                break;
            case EnemyState.Chase:
                if (agent != null)
                    agent.speed = chaseSpeed;
                if (dangerEffectController != null)
                    dangerEffectController.StartDangerEffect();
                GetAudioManager()?.PlayHeartbeat();
                break;
            case EnemyState.Flee:
                fleeTimer = 0f;
                if (agent != null)
                {
                    agent.speed = fleeSpeed;
                    agent.SetDestination(GetFleeTarget());
                }
                if (dangerEffectController != null)
                    dangerEffectController.StopDangerEffect();
                break;
            case EnemyState.ReturnToPatrol:
                currentPatrolIndex = FindNearestPatrolIndex();
                if (agent != null)
                {
                    agent.speed = patrolSpeed;
                    SetCurrentPatrolDestination();
                }
                break;
        }
    }

    private void PlayStateAnimation(EnemyState state)
    {
        if (animator == null)
            return;

        switch (state)
        {
            case EnemyState.Patrol:
                CrossFadeIfStateExists(patrolAnimationState, baseLayerIndex);
                CrossFadeUpperLayerToEmpty();
                break;

            case EnemyState.Chase:
                CrossFadeIfStateExists(chaseAnimationState, baseLayerIndex);
                CrossFadeUpperLayerToEmpty();
                break;

            case EnemyState.Flee:
                CrossFadeIfStateExists(fleeAnimationState, baseLayerIndex);

                if (useUpperLayerForFlee)
                    CrossFadeIfStateExists(upperFleeAnimationState, upperLayerIndex);

                break;

            case EnemyState.ReturnToPatrol:
                CrossFadeIfStateExists(returnToPatrolAnimationState, baseLayerIndex);
                CrossFadeUpperLayerToEmpty();
                break;
        }
    }

    private void CrossFadeUpperLayerToEmpty()
    {
        if (!useUpperLayerForFlee)
            return;

        CrossFadeIfStateExists(upperEmptyAnimationState, upperLayerIndex);
    }

    private void CrossFadeIfStateExists(string stateName, int layerIndex)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        if (layerIndex < 0 || layerIndex >= animator.layerCount)
            return;

        int shortNameHash = Animator.StringToHash(stateName);
        string layerName = animator.GetLayerName(layerIndex);
        int fullPathHash = Animator.StringToHash($"{layerName}.{stateName}");

        if (!animator.HasState(layerIndex, shortNameHash) && !animator.HasState(layerIndex, fullPathHash))
        {
            if (logStateChanges)
                Debug.LogWarning($"EnemyAI: Animator state '{stateName}' was not found on layer {layerIndex} ({layerName}).");

            return;
        }

        animator.CrossFade(stateName, animationFadeTime, layerIndex);
    }

    private void ExitState(EnemyState state)
    {
        if (state != EnemyState.Chase)
            return;

        if (dangerEffectController != null)
            dangerEffectController.StopDangerEffect();

        GetAudioManager()?.StopHeartbeat();
    }

    private void UpdatePatrol()
    {
        if (CanDetectPlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!HasPatrolPath())
            return;

        if (Vector3.Distance(transform.position, patrolPositions[currentPatrolIndex]) > patrolPointReachDistance)
            return;

        AdvancePatrolIndex();
        SetCurrentPatrolDestination();
    }

    private void UpdateChase()
    {
        if (CheckFlashlightRepel())
        {
            ChangeState(EnemyState.Flee);
            return;
        }

        if (player != null && agent != null)
            agent.SetDestination(player.position);
    }

    private void UpdateFlee()
    {
        fleeTimer += Time.deltaTime;

        if (fleeTimer < fleeDuration)
            return;

        ChangeState(HasPatrolPath() ? EnemyState.ReturnToPatrol : EnemyState.Patrol);
    }

    private void UpdateReturnToPatrol()
    {
        if (!HasPatrolPath())
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (Vector3.Distance(transform.position, patrolPositions[currentPatrolIndex]) <= patrolPointReachDistance)
            ChangeState(EnemyState.Patrol);
    }

    private bool CanDetectPlayer()
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= detectRange;
    }

    private bool CheckFlashlightRepel()
    {
        if (flashlightRepelRaycaster == null)
            return false;

        if (!flashlightRepelRaycaster.TryGetHitFace(out RaycastHit hit))
            return false;

        EnemyFaceHitBox hitFace = hit.collider.GetComponent<EnemyFaceHitBox>();

        if (hitFace == null)
            hitFace = hit.collider.GetComponentInParent<EnemyFaceHitBox>();

        if (hitFace == null)
            return false;

        return hitFace.GetOwnerEnemy() == this;
    }

    private void CachePatrolPositions()
    {
        if (patrolLine == null || patrolLine.positionCount <= 0)
        {
            patrolPositions = new Vector3[0];
            return;
        }

        patrolPositions = new Vector3[patrolLine.positionCount];

        for (int i = 0; i < patrolLine.positionCount; i++)
        {
            Vector3 point = patrolLine.GetPosition(i);
            patrolPositions[i] = patrolLine.useWorldSpace
                ? point
                : patrolLine.transform.TransformPoint(point);
        }

        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPositions.Length - 1);
    }

    private void SetCurrentPatrolDestination()
    {
        if (agent == null || !HasPatrolPath())
            return;

        agent.SetDestination(patrolPositions[currentPatrolIndex]);
    }

    private void AdvancePatrolIndex()
    {
        if (!HasPatrolPath())
            return;

        if (currentPatrolIndex < patrolPositions.Length - 1)
        {
            currentPatrolIndex++;
            return;
        }

        if (loopPatrol)
            currentPatrolIndex = 0;
    }

    private Vector3 GetFleeTarget()
    {
        if (player == null || agent == null)
            return transform.position;

        Vector3 fleeDirection = (transform.position - player.position).normalized;

        if (fleeDirection.sqrMagnitude <= 0.001f)
            fleeDirection = -transform.forward;

        Vector3 rawTarget = transform.position + fleeDirection * fleeDistance;

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();

            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                return hit.position;
        }

        return transform.position;
    }

    private int FindNearestPatrolIndex()
    {
        if (!HasPatrolPath())
            return 0;

        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < patrolPositions.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPositions[i]);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private bool HasPatrolPath()
    {
        return patrolPositions != null && patrolPositions.Length > 0;
    }

    private AudioManager GetAudioManager()
    {
        return audioManager != null ? audioManager : AudioManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other != null ? other.gameObject : null);
    }

    private void OnTriggerStay(Collider other)
    {
        TryKillPlayer(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision != null ? collision.gameObject : null);
    }

    private void TryKillPlayer(GameObject other)
    {
        if (hasTouchedPlayer || other == null)
            return;

        PlayerDeathHandler deathHandler = playerDeathHandler;

        if (deathHandler == null)
            deathHandler = other.GetComponentInParent<PlayerDeathHandler>();

        if (deathHandler == null && player != null)
        {
            deathHandler = player.GetComponentInParent<PlayerDeathHandler>();

            if (deathHandler == null)
                deathHandler = player.GetComponentInChildren<PlayerDeathHandler>();
        }

        bool isAssignedPlayer = player != null
            && (other.transform == player || other.transform.IsChildOf(player));
        bool isTaggedPlayer = !string.IsNullOrEmpty(playerTag) && other.tag == playerTag;

        if (!isAssignedPlayer && !isTaggedPlayer && deathHandler == null)
            return;

        hasTouchedPlayer = true;

        if (deathHandler != null)
            deathHandler.KillPlayer();
        else
            Debug.LogWarning("EnemyAI: playerDeathHandler is not assigned and could not be found on the player.");
    }

    private void ValidateReferences()
    {
        if (agent == null)
            Debug.LogWarning("EnemyAI: agent is not assigned.");

        if (player == null)
            Debug.LogWarning("EnemyAI: player is not assigned.");

        if (flashlightRepelRaycaster == null)
            Debug.LogWarning("EnemyAI: flashlightRepelRaycaster is not assigned.");

        if (faceHitBox == null)
            Debug.LogWarning("EnemyAI: faceHitBox is not assigned.");

        if (animator == null)
            Debug.LogWarning("EnemyAI: animator is not assigned and could not be found in children.");
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}

