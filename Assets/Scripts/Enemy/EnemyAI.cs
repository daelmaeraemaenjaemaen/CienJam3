using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Activation")]
    [SerializeField] private bool canMove = true;

    [Header("Patrol Line")]
    [SerializeField] private LineRenderer patrolLine;
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float patrolPointReachDistance = 0.5f;
    [SerializeField] private bool loopPatrol = true;

    [Header("Chase")]
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float chaseSpeed = 2.0f;
    [SerializeField] private ChaseDangerEffectController dangerEffectController;

    [Header("Flee")]
    [SerializeField] private FlashlightRepelRaycaster flashlightRepelRaycaster;
    [SerializeField] private EnemyFaceHitBox faceHitBox;
    [SerializeField] private float fleeSpeed = 2.8f;
    [SerializeField] private float fleeDuration = 5f;
    [SerializeField] private float fleeDistance = 6f;
    [SerializeField] private float navMeshSampleRadius = 3f;

    [Header("Agent Tuning")]
    [SerializeField] private float destinationUpdateInterval = 0.2f;
    [SerializeField] private float destinationRefreshDistance = 0.25f;
    [SerializeField] private float agentAcceleration = 6f;
    [SerializeField] private float agentAngularSpeed = 220f;
    [SerializeField] private float agentStoppingDistance = 0.1f;
    [SerializeField] private float navMeshWarpSearchRadius = 2f;

    [Header("Runtime Movement Clamp")]
    [SerializeField] private bool useRuntimeMovementClamp = true;
    [SerializeField] private float maxPatrolSpeed = 1.2f;
    [SerializeField] private float maxChaseSpeed = 2.0f;
    [SerializeField] private float maxFleeSpeed = 2.8f;
    [SerializeField] private float maxContactKillDistance = 0.85f;
    [SerializeField] private float maxAgentAcceleration = 6f;
    [SerializeField] private float maxAgentAngularSpeed = 220f;

    [Header("Death")]
    [SerializeField] private PlayerDeathHandler playerDeathHandler;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float contactKillDistance = 0.85f;
    [SerializeField] private LayerMask contactObstructionLayerMask = ~0;
    [SerializeField] private float contactLinecastHeightOffset = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioManager audioManager;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationFadeTime = 0.15f;
    [SerializeField] private int baseLayerIndex = 0;
    [SerializeField] private int upperLayerIndex = 1;
    [SerializeField] private bool useUpperLayerForFlee = true;
    [SerializeField] private bool disableRootMotionAtRuntime = true;
    [SerializeField] private string idleAnimationState = "Patrol";
    [SerializeField] private string walkAnimationState = "Patrol";
    [SerializeField] private string runAnimationState = "Chase";
    [SerializeField] private string fallbackStateName = "Patrol";
    [SerializeField] private float idleAnimatorSpeed = 1f;
    [SerializeField] private float walkAnimatorSpeed = 1f;
    [SerializeField] private float runAnimatorSpeed = 1.1f;
    [SerializeField] private float pausedAnimatorSpeed = 0f;
    [SerializeField] private float movingAnimationVelocityThreshold = 0.05f;
    [Tooltip("Legacy fallback. If Idle/Walk/Run names do not exist, set the fields above to the real Animator state names.")]
    [SerializeField] private string patrolAnimationState = "Patrol";
    [SerializeField] private string chaseAnimationState = "Chase";
    [SerializeField] private string fleeAnimationState = "Flee";
    [SerializeField] private string returnToPatrolAnimationState = "ReturnToPatrol";
    [SerializeField] private string upperEmptyAnimationState = "UpperEmpty";
    [SerializeField] private string upperFleeAnimationState = "Flee";

    [Header("Debug")]
    [SerializeField] private bool drawDebugRange = true;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private bool logContactKillDistance = true;

    private EnemyState currentState;
    private Vector3[] patrolPositions = new Vector3[0];
    private int currentPatrolIndex;
    private float fleeTimer;
    private bool hasTouchedPlayer;
    private bool hasEnteredInitialState;
    private bool hasLoggedAgentNotOnNavMesh;
    private bool hasTriedNavMeshWarp;
    private bool hasLoggedAnimatorDisabled;
    private bool hasLoggedAnimatorControllerMissing;
    private bool hasLoggedAnimatorAvatarMissing;
    private float contactKillDistanceLogTimer;
    private float lastDestinationSetTime = -999f;
    private Vector3 lastDestination;
    private bool hasLastDestination;
    private string currentBaseAnimationState;
    private readonly HashSet<string> missingAnimatorStateWarnings = new HashSet<string>();

    public EnemyState CurrentState => currentState;
    public bool CanMove => canMove;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ValidateReferences();
        ApplyAgentTuning();
    }

    private void Start()
    {
        CachePatrolPositions();
        TryPlaceAgentOnNavMeshOnce();

        if (canMove)
        {
            SetAnimatorSpeed(1f);
            SetAgentStopped(false);
            ChangeState(EnemyState.Patrol);
        }
        else
        {
            SetAnimatorSpeed(pausedAnimatorSpeed);
            SetAgentStopped(true);
        }
    }

    private void Update()
    {
        if (agent == null)
            return;

        if (!canMove)
        {
            SetAgentStopped(true);
            SetAnimatorSpeed(pausedAnimatorSpeed);
            return;
        }

        if (!agent.isOnNavMesh)
        {
            if (TryPlaceAgentOnNavMeshOnce())
                return;

            LogAgentNotOnNavMeshWarning();
            return;
        }

        ApplyAgentTuning();

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

        UpdateMovementAnimation(false);
        LogContactKillDistanceDebug();
        TryKillAssignedPlayerByDistance();
    }

    public void RefreshPatrolPath()
    {
        CachePatrolPositions();
    }

    public void SetCanMove(bool value)
    {
        if (canMove == value)
        {
            if (value)
            {
                SetAnimatorSpeed(1f);
                UpdateMovementAnimation(true);
            }

            return;
        }

        canMove = value;
        SetAgentStopped(!canMove);

        if (!canMove)
        {
            SetAnimatorSpeed(pausedAnimatorSpeed);

            if (dangerEffectController != null)
                dangerEffectController.StopDangerEffect();

            GetAudioManager()?.StopHeartbeat();
            return;
        }

        SetAnimatorSpeed(1f);
        ValidateAnimatorRuntimeState();
        TryPlaceAgentOnNavMeshOnce();

        if (!hasEnteredInitialState)
            ChangeState(EnemyState.Patrol);
        else
            UpdateMovementAnimation(true);
    }

    public void StartPatrol()
    {
        SetCanMove(true);
        SetAnimatorSpeed(1f);
        UpdateMovementAnimation(true);
    }

    public void EnableAI()
    {
        SetCanMove(true);
        SetAnimatorSpeed(1f);
        UpdateMovementAnimation(true);
    }

    public void RequestFleeFromLight()
    {
        if (!canMove || currentState == EnemyState.Flee)
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
        ResetDestinationThrottle();
        EnterState(nextState);

        if (logStateChanges)
            Debug.Log($"EnemyAI: {previousState} -> {nextState}");
    }

    private void EnterState(EnemyState state)
    {
        ApplyAgentTuning();
        SetAgentStopped(false);
        PlayStateAnimation(state);

        switch (state)
        {
            case EnemyState.Patrol:
                if (agent != null)
                    agent.speed = GetRuntimePatrolSpeed();
                SetCurrentPatrolDestination();
                break;
            case EnemyState.Chase:
                if (agent != null)
                    agent.speed = GetRuntimeChaseSpeed();
                if (dangerEffectController != null)
                    dangerEffectController.StartDangerEffect();
                GetAudioManager()?.PlayHeartbeat();
                break;
            case EnemyState.Flee:
                fleeTimer = 0f;
                if (agent != null)
                {
                    agent.speed = GetRuntimeFleeSpeed();
                    SetAgentDestination(GetFleeTarget(), true);
                }
                if (dangerEffectController != null)
                    dangerEffectController.StopDangerEffect();
                break;
            case EnemyState.ReturnToPatrol:
                currentPatrolIndex = FindNearestPatrolIndex();
                if (agent != null)
                {
                    agent.speed = GetRuntimePatrolSpeed();
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
                CrossFadeMovementState(true);
                CrossFadeUpperLayerToEmpty();
                break;

            case EnemyState.Chase:
                CrossFadeMovementState(true);
                CrossFadeUpperLayerToEmpty();
                break;

            case EnemyState.Flee:
                CrossFadeMovementState(true);

                if (useUpperLayerForFlee)
                    CrossFadeIfStateExists(upperFleeAnimationState, upperLayerIndex);

                break;

            case EnemyState.ReturnToPatrol:
                CrossFadeMovementState(true);
                CrossFadeUpperLayerToEmpty();
                break;
        }
    }

    private void UpdateMovementAnimation(bool forceRefresh)
    {
        if (animator == null || agent == null)
            return;

        if (!canMove || agent.isStopped || agent.velocity.sqrMagnitude <= movingAnimationVelocityThreshold * movingAnimationVelocityThreshold)
        {
            CrossFadeIdleState(forceRefresh);
            return;
        }

        CrossFadeMovementState(forceRefresh);
    }

    private void CrossFadeMovementState(bool forceRefresh)
    {
        if (!ValidateAnimatorRuntimeState())
            return;

        bool isFastMovement = currentState == EnemyState.Chase || currentState == EnemyState.Flee;
        SetAnimatorSpeed(isFastMovement ? runAnimatorSpeed : walkAnimatorSpeed);

        switch (currentState)
        {
            case EnemyState.Chase:
                if (TryCrossFadeBaseState(runAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(chaseAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(walkAnimationState, forceRefresh))
                    return;
                break;

            case EnemyState.Flee:
                if (TryCrossFadeBaseState(fleeAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(runAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(walkAnimationState, forceRefresh))
                    return;
                break;

            case EnemyState.ReturnToPatrol:
                if (TryCrossFadeBaseState(walkAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(returnToPatrolAnimationState, forceRefresh))
                    return;
                break;

            case EnemyState.Patrol:
            default:
                if (TryCrossFadeBaseState(walkAnimationState, forceRefresh))
                    return;
                if (TryCrossFadeBaseState(patrolAnimationState, forceRefresh))
                    return;
                break;
        }

        TryCrossFadeBaseState(fallbackStateName, forceRefresh);
    }

    private void CrossFadeIdleState(bool forceRefresh)
    {
        if (!ValidateAnimatorRuntimeState())
            return;

        SetAnimatorSpeed(idleAnimatorSpeed);

        if (TryCrossFadeBaseState(idleAnimationState, forceRefresh))
            return;

        TryCrossFadeBaseState(fallbackStateName, forceRefresh);
    }

    private void CrossFadeBaseState(string stateName, float animatorSpeed, bool forceRefresh = false)
    {
        if (!ValidateAnimatorRuntimeState())
            return;

        SetAnimatorSpeed(animatorSpeed);
        TryCrossFadeBaseState(stateName, forceRefresh);
    }

    private bool TryCrossFadeBaseState(string stateName, bool forceRefresh)
    {
        if (!forceRefresh && currentBaseAnimationState == stateName)
            return true;

        if (CrossFadeIfStateExists(stateName, baseLayerIndex))
        {
            currentBaseAnimationState = stateName;
            return true;
        }

        return false;
    }

    private void CrossFadeUpperLayerToEmpty()
    {
        if (!useUpperLayerForFlee)
            return;

        CrossFadeIfStateExists(upperEmptyAnimationState, upperLayerIndex);
    }

    private bool CrossFadeIfStateExists(string stateName, int layerIndex)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        if (layerIndex < 0 || layerIndex >= animator.layerCount)
            return false;

        string layerName = animator.GetLayerName(layerIndex);
        string warningKey = $"{layerIndex}:{stateName}";

        if (missingAnimatorStateWarnings.Contains(warningKey))
            return false;

        int shortNameHash = Animator.StringToHash(stateName);
        int fullPathHash = Animator.StringToHash($"{layerName}.{stateName}");

        if (!animator.HasState(layerIndex, shortNameHash) && !animator.HasState(layerIndex, fullPathHash))
        {
            if (logStateChanges && missingAnimatorStateWarnings.Add(warningKey))
                Debug.LogWarning($"EnemyAI: Animator state '{stateName}' was not found on layer {layerIndex} ({layerName}).");

            return false;
        }

        animator.CrossFade(stateName, animationFadeTime, layerIndex);
        return true;
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
            SetAgentDestination(player.position, false);
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

        SetAgentDestination(patrolPositions[currentPatrolIndex], true);
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

        Vector3 awayFromPlayer = (transform.position - player.position).normalized;

        if (awayFromPlayer.sqrMagnitude <= 0.001f)
            awayFromPlayer = -transform.forward;

        float currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        float[] fleeAngleCandidates = { 0f, -30f, 30f, -60f, 60f, -90f, 90f };
        Vector3 bestTarget = transform.position;
        float bestScore = float.MinValue;
        Vector3 fallbackTarget = transform.position;
        float fallbackScore = float.MinValue;

        for (int i = 0; i < fleeAngleCandidates.Length; i++)
        {
            Vector3 rotatedDirection = Quaternion.Euler(0f, fleeAngleCandidates[i], 0f) * awayFromPlayer;
            Vector3 rawTarget = transform.position + rotatedDirection * fleeDistance;

            if (!NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            float candidateDistanceToPlayer = Vector3.Distance(hit.position, player.position);
            float travelDistance = Vector3.Distance(transform.position, hit.position);
            Vector3 moveDir = (hit.position - transform.position).normalized;
            float awayDot = Vector3.Dot(moveDir, awayFromPlayer);
            float score = candidateDistanceToPlayer + awayDot * 3f;

            if (candidateDistanceToPlayer > fallbackScore)
            {
                fallbackScore = candidateDistanceToPlayer;
                fallbackTarget = hit.position;
            }

            if (candidateDistanceToPlayer <= currentDistanceToPlayer)
                continue;

            if (awayDot < 0.2f)
                continue;

            if (travelDistance < 0.5f)
                continue;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestTarget = hit.position;
        }

        if (bestScore > float.MinValue)
            return bestTarget;

        return fallbackScore > currentDistanceToPlayer ? fallbackTarget : transform.position;
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

    private void SetAgentStopped(bool stopped)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.isStopped = stopped;
    }

    private void ApplyAgentTuning()
    {
        if (agent == null)
            return;

        agent.acceleration = Mathf.Max(0.1f, GetRuntimeAgentAcceleration());
        agent.angularSpeed = Mathf.Max(1f, GetRuntimeAgentAngularSpeed());
        agent.stoppingDistance = Mathf.Max(0f, agentStoppingDistance);
    }

    private float GetRuntimePatrolSpeed()
    {
        return useRuntimeMovementClamp ? Mathf.Min(patrolSpeed, maxPatrolSpeed) : patrolSpeed;
    }

    private float GetRuntimeChaseSpeed()
    {
        return useRuntimeMovementClamp ? Mathf.Min(chaseSpeed, maxChaseSpeed) : chaseSpeed;
    }

    private float GetRuntimeFleeSpeed()
    {
        return useRuntimeMovementClamp ? Mathf.Min(fleeSpeed, maxFleeSpeed) : fleeSpeed;
    }

    private float GetRuntimeContactKillDistance()
    {
        return useRuntimeMovementClamp ? Mathf.Min(contactKillDistance, maxContactKillDistance) : contactKillDistance;
    }

    private float GetRuntimeAgentAcceleration()
    {
        return useRuntimeMovementClamp ? Mathf.Min(agentAcceleration, maxAgentAcceleration) : agentAcceleration;
    }

    private float GetRuntimeAgentAngularSpeed()
    {
        return useRuntimeMovementClamp ? Mathf.Min(agentAngularSpeed, maxAgentAngularSpeed) : agentAngularSpeed;
    }

    private void ResetDestinationThrottle()
    {
        hasLastDestination = false;
        lastDestinationSetTime = -999f;
    }

    private void SetAgentDestination(Vector3 destination, bool force)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (!force && hasLastDestination)
        {
            float elapsed = Time.time - lastDestinationSetTime;
            float sqrRefreshDistance = destinationRefreshDistance * destinationRefreshDistance;
            bool isSameDestination = (destination - lastDestination).sqrMagnitude <= sqrRefreshDistance;

            if (elapsed < destinationUpdateInterval || isSameDestination)
                return;
        }

        agent.SetDestination(destination);
        lastDestination = destination;
        lastDestinationSetTime = Time.time;
        hasLastDestination = true;
    }

    private bool TryPlaceAgentOnNavMeshOnce()
    {
        if (agent == null)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (hasTriedNavMeshWarp)
            return false;

        hasTriedNavMeshWarp = true;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshWarpSearchRadius, NavMesh.AllAreas))
            return false;

        float warpDistance = Vector3.Distance(transform.position, hit.position);

        if (!agent.Warp(hit.position))
            return false;

        hasLoggedAgentNotOnNavMesh = false;
        ResetDestinationThrottle();
        Debug.Log($"EnemyAI: NavMeshAgent was warped once to the nearest NavMesh position. Distance: {warpDistance:F2}");
        return true;
    }

    private void LogAgentNotOnNavMeshWarning()
    {
        if (hasLoggedAgentNotOnNavMesh)
            return;

        Debug.LogWarning("EnemyAI: NavMeshAgent is not on a NavMesh. Check Girlfriend position and baked NavMesh.");
        hasLoggedAgentNotOnNavMesh = true;
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

    private void LogContactKillDistanceDebug()
    {
        if (!logContactKillDistance || player == null)
            return;

        contactKillDistanceLogTimer += Time.deltaTime;
        if (contactKillDistanceLogTimer < 2f)
            return;

        contactKillDistanceLogTimer = 0f;
        float currentDistance = Vector3.Distance(transform.position, player.position);
        Debug.Log($"EnemyAI: Player distance {currentDistance:F2}, contactKillDistance {GetRuntimeContactKillDistance():F2}");
    }

    private void TryKillAssignedPlayerByDistance()
    {
        float runtimeContactKillDistance = GetRuntimeContactKillDistance();

        if (hasTouchedPlayer || player == null || runtimeContactKillDistance <= 0f)
            return;

        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        if (sqrDistance > runtimeContactKillDistance * runtimeContactKillDistance)
            return;

        if (!HasClearCatchLineToPlayer())
            return;

        TryKillPlayer(player.gameObject);
    }

    private bool HasClearCatchLineToPlayer()
    {
        Vector3 start = transform.position + Vector3.up * contactLinecastHeightOffset;
        Vector3 end = player.position + Vector3.up * contactLinecastHeightOffset;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction.normalized,
            distance,
            contactObstructionLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length <= 0)
            return true;

        SortHitsByDistance(hits);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || IsOwnCollider(hitCollider))
                continue;

            if (IsPlayerCollider(hitCollider))
                return true;

            return false;
        }

        return true;
    }

    private bool IsOwnCollider(Collider hitCollider)
    {
        return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
    }

    private bool IsPlayerCollider(Collider hitCollider)
    {
        if (player == null)
            return false;

        Transform hitTransform = hitCollider.transform;
        return hitTransform == player || hitTransform.IsChildOf(player);
    }

    private void SortHitsByDistance(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length - 1; i++)
        {
            for (int j = i + 1; j < hits.Length; j++)
            {
                if (hits[j].distance >= hits[i].distance)
                    continue;

                RaycastHit temp = hits[i];
                hits[i] = hits[j];
                hits[j] = temp;
            }
        }
    }

    private bool ValidateAnimatorRuntimeState()
    {
        if (animator == null)
            return false;

        if (!animator.enabled)
        {
            if (!hasLoggedAnimatorDisabled)
            {
                Debug.LogWarning("EnemyAI: Animator is disabled. Girlfriend can move, but animation will not play.");
                hasLoggedAnimatorDisabled = true;
            }

            return false;
        }

        if (animator.runtimeAnimatorController == null)
        {
            if (!hasLoggedAnimatorControllerMissing)
            {
                Debug.LogWarning("EnemyAI: Animator Controller is missing. Assign a controller on Girlfriend Animator.");
                hasLoggedAnimatorControllerMissing = true;
            }

            return false;
        }

        if (animator.avatar == null && !hasLoggedAnimatorAvatarMissing)
        {
            Debug.LogWarning("EnemyAI: Animator Avatar is missing. If the model is humanoid, walking/running animation may not play correctly.");
            hasLoggedAnimatorAvatarMissing = true;
        }

        if (disableRootMotionAtRuntime && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            Debug.LogWarning("EnemyAI: Animator Apply Root Motion was enabled and has been disabled at runtime so NavMeshAgent can control movement.");
        }

        return true;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null || !animator.enabled)
            return;

        animator.speed = Mathf.Max(0f, speed);
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
        else
            ValidateAnimatorRuntimeState();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRange)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}

