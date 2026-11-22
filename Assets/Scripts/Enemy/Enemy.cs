using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(HealthComp))]
public class Enemy : MonoBehaviour
{
    [Min(1f)] public float InvestigationTimeout = 20f;
    public Transform Target;
    public Transform Eye;
    public Transform[] PatrolPoints;
    [Min(1f)] public float RandomPatrolRadius = 10f;
    [Min(0.1f)] public float PatrolArrivalDistance = 0.35f;
    [Min(1f)] public float PatrolPointTimeout = 30f;
    public LayerMask ObstacleLayers;
    [Tooltip("Turn off temporarily to test patrol without player detection.")]
    public bool DetectPlayer = true;
    [SerializeField] private string currentBehavior;
    private float nextNavigationWarning;
    public float DetectionRange = 15f, LoseTargetRange = 22f, AttackRange = 2f;
    public float PatrolSpeed = 2f, ChaseSpeed = 4f, IdleDuration = 2f, MemoryDuration = 3f;
    public float AttackDamage = 10f, AttackCooldown = 1f, AttackWindup = 0.35f, DeathLifetime = 3f;
    public NavMeshAgent Agent { get; private set; }
    public HealthComp Health { get; private set; }
    public EnemyStateMachine StateMachine { get; private set; }
    public RangedEnemy RangedWeapon { get; private set; }
    public float AttackDistance => RangedWeapon != null ? Mathf.Max(1f, RangedWeapon.ShootingRange) : AttackRange;
    public bool IsDead => Health.Health <= 0f;
    public bool CanNavigate => Agent != null && Agent.enabled && Agent.isOnNavMesh;
    public bool HasTarget => Target != null && Target.gameObject.activeInHierarchy &&
        (Target.GetComponent<EnemyPlayerHealth>() == null || !Target.GetComponent<EnemyPlayerHealth>().IsDead);
    public float TargetDistance => HasTarget ? Vector3.Distance(transform.position, Target.position) : float.PositiveInfinity;
    public Vector3 LastSeenPosition { get; private set; }
    public float LastSeenTime { get; private set; } = float.NegativeInfinity;
    public int PatrolIndex { get; set; }
    public float NextAttackTime { get; set; }

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Health = GetComponent<HealthComp>();
        if (GetComponent<HealthBarDisplay>() == null) gameObject.AddComponent<HealthBarDisplay>();
        RangedWeapon = GetComponent<RangedEnemy>();
        StateMachine = new EnemyStateMachine(this);
    }
    private void Start()
    {
        if (Target == null)
        {
            Player player = FindAnyObjectByType<Player>();
            if (player != null) Target = player.transform;
        }
        if (!CanNavigate) Debug.LogWarning("Place enemy on a baked NavMesh.", this);
    }
    private void Update()
    {
        if (CanSeeTarget()) { LastSeenPosition = Target.position; LastSeenTime = Time.time; }
        StateMachine.Update();
        currentBehavior = StateMachine.CurrentState.GetType().Name;
    }
    private void FixedUpdate() => StateMachine.FixedUpdate();
    private void OnDisable() => StopMoving();
    public bool CanSeeTarget()
    {
        if (!DetectPlayer || !HasTarget || TargetDistance > Mathf.Max(DetectionRange, LoseTargetRange)) return false;
        Vector3 origin = Eye != null ? Eye.position : transform.position + Vector3.up * 0.5f;
        return !Physics.Linecast(origin, Target.position + Vector3.up * 0.5f,
            ObstacleLayers, QueryTriggerInteraction.Ignore);
    }
    public bool DetectTarget() => TargetDistance <= DetectionRange && CanSeeTarget();
    public bool RemembersTarget => HasTarget && TargetDistance <= Mathf.Max(DetectionRange, LoseTargetRange) &&
        Time.time - LastSeenTime <= MemoryDuration;
    public void MoveTo(Vector3 position, float speed, float stoppingDistance)
    {
        if (!CanNavigate) return;
        Agent.speed = Mathf.Max(0f, speed);
        Agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
        Agent.isStopped = false;
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, Agent.areaMask))
        { NavigationWarning("No NavMesh within 2 units of destination " + position); Agent.ResetPath(); return; }
        NavMeshPath path = new NavMeshPath();
        if (Agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            Agent.SetPath(path);
        else { NavigationWarning("No complete NavMesh path to destination " + position); Agent.ResetPath(); }
    }
    public void StopMoving()
    {
        if (!CanNavigate) return;
        Agent.isStopped = true;
        Agent.ResetPath();
    }
    public void FaceTarget()
    {
        if (!HasTarget) return;
        Vector3 direction = Target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), 360f * Time.deltaTime);
    }
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f) return;
        EnemyHitFlash flash = GetComponent<EnemyHitFlash>();
        if (flash == null) flash = gameObject.AddComponent<EnemyHitFlash>();
        flash.Flash();
        CharacterSounds.Play(this, "Hurt");
        Health.Health = Mathf.Max(0f, Health.Health - damage);
        if (IsDead) StateMachine.TryChangeState(StateMachine.CurrentState, EnemyStateType.Dead);
    }
    public void DealDamage()
    {
        if (!DetectPlayer || !HasTarget || TargetDistance > AttackRange || !CanSeeTarget()) return;
        EnemyPlayerHealth health = Target.GetComponent<EnemyPlayerHealth>();
        CharacterSounds.Play(this, "MeleeAttack");
        if (health != null) health.TakeDamage(AttackDamage);
    }
    public bool HasPatrolPoint()
    {
        if (PatrolPoints == null || PatrolPoints.Length == 0) return false;
        for (int i = 0; i < PatrolPoints.Length; i++)
        {
            PatrolIndex %= PatrolPoints.Length;
            if (PatrolPoints[PatrolIndex] != null) return true;
            PatrolIndex++;
        }
        return false;
    }
    public void Die()
    {
        StopMoving();
        CharacterSounds.Play(this, "Death");
        Agent.enabled = false;
        // Stop animation from driving transforms while physics owns the corpse.
        foreach (Animator animator in GetComponentsInChildren<Animator>())
            animator.enabled = false;
        RangedEnemy weapon = GetComponent<RangedEnemy>();
        if (weapon != null) weapon.enabled = false;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.None;
        body.detectCollisions = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Keep the existing solid colliders so the body lands on the floor.
        // A small angular impulse lets an upright capsule topple naturally.
        body.AddTorque(transform.right * 2f, ForceMode.VelocityChange);
        Destroy(gameObject, Mathf.Max(0f, DeathLifetime));
    }
    private void OnDrawGizmosSelected()
    {
        if (PatrolPoints == null) return;
        Gizmos.color = Color.cyan;
        Transform first = null;
        Transform previous = null;
        foreach (Transform point in PatrolPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, Mathf.Max(0.1f, PatrolArrivalDistance));
            if (previous != null) Gizmos.DrawLine(previous.position, point.position);
            if (first == null) first = point;
            previous = point;
        }
        if (first != null && previous != first) Gizmos.DrawLine(previous.position, first.position);
    }
    private void NavigationWarning(string message)
    {
        if (Time.time < nextNavigationWarning) return;
        nextNavigationWarning = Time.time + 5f;
        Debug.LogWarning(message, this);
    }
    public bool TryRandomPatrolPoint(out Vector3 destination)
    {
        destination = transform.position;
        if (!CanNavigate) return false;
        NavMeshPath path = new NavMeshPath();
        float radius = Mathf.Max(1f, RandomPatrolRadius);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = Agent.nextPosition + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, Agent.areaMask)) continue;
            if (Vector3.Distance(Agent.nextPosition, hit.position) <
                Mathf.Max(1f, PatrolArrivalDistance * 2f)) continue;
            if (!Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            destination = hit.position;
            return true;
        }
        return false;
    }
}