using UnityEngine;

public class EnemyPatrolState : EnemyState
{
    private float nextPathTime;
    private float giveUpTime;
    private Vector3 randomDestination;
    private bool randomReady;

    public EnemyPatrolState(Enemy context) : base(context) { }

    public override void EnterState()
    {
        nextPathTime = 0f;
        randomReady = false;
        giveUpTime = Time.time + Mathf.Max(1f, context.PatrolPointTimeout);
    }

    public override void ExitState() => context.StopMoving();

    public override void UpdateState()
    {
        if (context.DetectTarget())
        {
            context.StateMachine.TryChangeState(this, EnemyStateType.Chase);
            return;
        }
        bool assignedRoute = context.HasPatrolPoint();
        if (!assignedRoute && !randomReady)
            randomReady = context.TryRandomPatrolPoint(out randomDestination);
        if (!assignedRoute && !randomReady)
        {
            context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
            return;
        }
        Vector3 destination = assignedRoute ? context.PatrolPoints[context.PatrolIndex].position : randomDestination;
        float arrival = Mathf.Max(0.1f, context.PatrolArrivalDistance);
        bool arrived = context.CanNavigate && !context.Agent.pathPending &&
            context.Agent.hasPath && context.Agent.remainingDistance <= arrival;

        // At a destination the agent may clear its path. Check the sampled position too.
        if (!arrived && context.CanNavigate &&
            UnityEngine.AI.NavMesh.SamplePosition(destination, out UnityEngine.AI.NavMeshHit hit,
                2f, context.Agent.areaMask))
            arrived = Vector3.Distance(context.Agent.nextPosition, hit.position) <= arrival;

        if (arrived || Time.time >= giveUpTime)
        {
            if (assignedRoute) context.PatrolIndex = (context.PatrolIndex + 1) % context.PatrolPoints.Length;
            context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
            return;
        }
        if (Time.time >= nextPathTime)
        {
            context.MoveTo(destination, context.PatrolSpeed, arrival * 0.7f);
            nextPathTime = Time.time + 0.5f;
        }
    }
}
