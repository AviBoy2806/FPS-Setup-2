public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Enemy context) : base(context) { }
    public override void EnterState() => context.Die();
    public override void ExitState() { }
    public override void UpdateState() { }
}