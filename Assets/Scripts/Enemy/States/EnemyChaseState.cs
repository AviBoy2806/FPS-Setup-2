using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseState : EnemyState
{
    private float nextPathTime;
    private float searchStarted;
    private float arrivedTime;
    private float trackedSeenTime;

    public EnemyChaseState(Enemy context) : base(context) { }

    public override void EnterState()
    {
        nextPathTime = 0f;
        searchStarted = Time.time;
        arrivedTime = -1f;
        trackedSeenTime = context.LastSeenTime;
    }

    public override void ExitState() => context.StopMoving();

    public override void UpdateState()
    {
        if (!context.HasTarget || float.IsNegativeInfinity(context.LastSeenTime))
        {
            context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
            return;
        }

        bool visible = context.CanSeeTarget();
        if (visible)
        {
            searchStarted = Time.time;
            arrivedTime = -1f;
            if (context.TargetDistance <= context.AttackDistance)
            {
                context.StateMachine.TryChangeState(this, EnemyStateType.Attack);
                return;
            }
        }
        if (trackedSeenTime != context.LastSeenTime)
        {
            trackedSeenTime = context.LastSeenTime;
            arrivedTime = -1f;
        }

        if (!visible)
        {
            // Investigate the remembered location, regardless of the hidden player's
            // current distance. Do not use the weapon's firing range as a stopping distance.
            bool arrived = false;
            if (context.CanNavigate)
            {
                arrived = !context.Agent.pathPending && context.Agent.hasPath &&
                    context.Agent.remainingDistance <= 0.55f;
                if (!arrived && NavMesh.SamplePosition(context.LastSeenPosition,
                    out NavMeshHit hit, 2f, context.Agent.areaMask))
                {
                    Vector3 delta = context.Agent.nextPosition - hit.position;
                    arrived = new Vector2(delta.x, delta.z).sqrMagnitude <= 0.55f * 0.55f &&
                        Mathf.Abs(delta.y) < 2f;
                }
            }
            if (arrived)
            {
                context.StopMoving();
                if (arrivedTime < 0f) arrivedTime = Time.time;
                if (Time.time - arrivedTime >= Mathf.Max(0f, context.MemoryDuration))
                    context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
                return;
            }
            // Bound pursuit when the remembered location cannot be reached.
            if (Time.time - searchStarted >= Mathf.Max(1f, context.InvestigationTimeout))
            {
                context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
                return;
            }
        }
        if (Time.time >= nextPathTime)
        {
            context.MoveTo(context.LastSeenPosition, context.ChaseSpeed,
                visible ? context.AttackDistance * 0.7f : 0.3f);
            nextPathTime = Time.time + 0.2f;
        }
    }
}
