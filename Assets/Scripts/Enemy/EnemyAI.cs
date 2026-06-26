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

    [Header("Debug")]
    [SerializeField] private bool drawDebugRange = true;
    [SerializeField] private bool logStateChanges = true;

    private EnemyState currentState;
    private Vector3[] patrolPositions;
    private int currentPatrolIndex;
    private float fleeTimer;
    private bool hasTouchedPlayer;
    private bool hasEnteredInitialState;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
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
            Debug.LogWarning("EnemyAI: NavMeshAgent is not on a NavMesh.");
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
                AudioManager.Instance?.PlayHeartbeat();
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

    private void ExitState(EnemyState state)
    {
        if (state != EnemyState.Chase)
            return;

        if (dangerEffectController != null)
            dangerEffectController.StopDangerEffect();

        AudioManager.Instance?.StopHeartbeat();
    }

    private void UpdatePatrol()
    {
        if (CanDetectPlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (patrolPositions == null || patrolPositions.Length == 0)
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

        if (player != null)
            agent.SetDestination(player.position);
    }

    private void UpdateFlee()
    {
        fleeTimer += Time.deltaTime;

        if (fleeTimer >= fleeDuration)
            ChangeState(EnemyState.ReturnToPatrol);
    }

    private void UpdateReturnToPatrol()
    {
        if (patrolPositions == null || patrolPositions.Length == 0)
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
        if (patrolLine == null || patrolLine.positionCount == 0)
        {
            Debug.LogWarning("EnemyAI needs a patrolLine with at least one position.");
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
        if (agent == null || patrolPositions == null || patrolPositions.Length == 0)
            return;

        agent.SetDestination(patrolPositions[currentPatrolIndex]);
    }

    private void AdvancePatrolIndex()
    {
        if (patrolPositions == null || patrolPositions.Length == 0)
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

        Debug.LogWarning("EnemyAI: could not find a valid flee target on NavMesh.");
        return transform.position;
    }

    private int FindNearestPatrolIndex()
    {
        if (patrolPositions == null || patrolPositions.Length == 0)
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

    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject other)
    {
        if (hasTouchedPlayer || other == null)
            return;

        bool isPlayer = other.CompareTag(playerTag)
            || (player != null && (other.transform == player || other.transform.IsChildOf(player)));

        if (!isPlayer)
            return;

        hasTouchedPlayer = true;

        if (playerDeathHandler != null)
            playerDeathHandler.KillPlayer();
        else
            Debug.LogWarning("EnemyAI: playerDeathHandler is not assigned.");
    }

    private void ValidateReferences()
    {
        if (agent == null)
            Debug.LogWarning("EnemyAI: agent is not assigned.");

        if (player == null)
            Debug.LogWarning("EnemyAI: player is not assigned.");

        if (patrolLine == null)
            Debug.LogWarning("EnemyAI: patrolLine is not assigned.");

        if (flashlightRepelRaycaster == null)
            Debug.LogWarning("EnemyAI: flashlightRepelRaycaster is not assigned.");

        if (faceHitBox == null)
            Debug.LogWarning("EnemyAI: faceHitBox is not assigned.");

        if (dangerEffectController == null)
            Debug.LogWarning("EnemyAI: dangerEffectController is not assigned.");

        if (playerDeathHandler == null)
            Debug.LogWarning("EnemyAI: playerDeathHandler is not assigned.");
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
