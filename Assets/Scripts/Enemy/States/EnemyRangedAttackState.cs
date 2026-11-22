using UnityEngine;

public class EnemyRangedAttackState : EnemyState
{
    private readonly RangedEnemy weapon;
    private float nextPathTime;
    private bool previousUpdateRotation;

    public EnemyRangedAttackState(Enemy context, RangedEnemy weapon) : base(context)
    {
        this.weapon = weapon;
    }

    public override void EnterState()
    {
        context.StopMoving();
        weapon.BeginAttack();
        nextPathTime = 0f;
        previousUpdateRotation = context.Agent.updateRotation;
        context.Agent.updateRotation = false;
    }

    public override void ExitState()
    {
        context.StopMoving();
        weapon.CancelAttack();
        context.Agent.updateRotation = previousUpdateRotation;
    }

    public override void UpdateState()
    {
        if (!context.RemembersTarget)
        {
            context.StateMachine.TryChangeState(this, EnemyStateType.Idle);
            return;
        }
        if (!context.CanSeeTarget() || context.TargetDistance > context.AttackDistance)
        {
            context.StateMachine.TryChangeState(this, EnemyStateType.Chase);
            return;
        }
        context.FaceTarget();
        if (!weapon.enabled) return;
        float preferred = Mathf.Clamp(weapon.PreferredDistance, 1f, context.AttackDistance * 0.85f);
        float retreat = Mathf.Clamp(weapon.RetreatDistance, 0f, preferred);
        if (context.TargetDistance < retreat)
        {
            if (Time.time >= nextPathTime)
            {
                Vector3 away = context.transform.position - context.Target.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -context.transform.forward;
                context.MoveTo(context.transform.position + away.normalized * (preferred - context.TargetDistance),
                    context.ChaseSpeed, 0.3f);
                nextPathTime = Time.time + 0.4f;
            }
        }
        else if (context.TargetDistance > preferred + 1f || !weapon.HasClearShot())
        {
            if (Time.time >= nextPathTime)
            {
                context.MoveTo(context.Target.position, context.ChaseSpeed, weapon.HasClearShot() ? preferred : 0.5f);
                nextPathTime = Time.time + 0.4f;
            }
        }
        else context.StopMoving();

        Vector3 direction = context.Target.position - context.transform.position;
        direction.y = 0f;
        // Turn toward the player before firing; no instant backwards shots.
        weapon.TickAttack(Vector3.Angle(context.transform.forward, direction) < 10f);
    }
}
