using UnityEngine;
public class EnemyIdleState : EnemyState
{
    private float finishTime;
    public EnemyIdleState(Enemy context) : base(context) { }
    public override void EnterState()
    {
        context.StopMoving();
        finishTime = Time.time + context.IdleDuration;
    }
    public override void ExitState() { }
    public override void UpdateState()
    {
        if (context.DetectTarget())
        { context.StateMachine.TryChangeState(this, EnemyStateType.Chase); return; }
        if (Time.time >= finishTime)
            context.StateMachine.TryChangeState(this, EnemyStateType.Patrol);
    }
}