public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player context) : base(context) { }
    public override void EnterState() => context.StopForDeath();
    public override void ExitState() { }
    public override void UpdateState() { }
}
