using UnityEngine;
public class EnemyAttackState : EnemyState
{
    private bool windingUp;
    private float hitTime;
    public EnemyAttackState(Enemy context) : base(context) { }
    public override void EnterState() { context.StopMoving(); windingUp = false; }
    public override void ExitState() { windingUp = false; }
    public override void UpdateState()
    {
        if (!context.RemembersTarget)
        { context.StateMachine.TryChangeState(this, EnemyStateType.Idle); return; }
        if (context.TargetDistance > context.AttackRange || !context.CanSeeTarget())
        { context.StateMachine.TryChangeState(this, EnemyStateType.Chase); return; }
        context.FaceTarget();
        if (!windingUp && Time.time >= context.NextAttackTime)
        {
            windingUp = true;
            hitTime = Time.time + Mathf.Max(0f, context.AttackWindup);
            context.NextAttackTime = hitTime + Mathf.Max(0.05f, context.AttackCooldown);
        }
        if (windingUp && Time.time >= hitTime) { windingUp = false; context.DealDamage(); }
    }
}