public enum PlayerStateType
{
    Idle,
    Walk,
    Jump,
    Fall,
    Dead
}

public abstract class PlayerState
{
    protected readonly Player context;

    protected PlayerState(Player context)
    {
        this.context = context;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();

    public virtual void FixedUpdateState()
    {
    }
}