public enum GunStateType
{
    Idle,
    Shooting,
    Reloading
}

public abstract class GunState
{
    protected readonly Gun context;

    protected GunState(Gun context)
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