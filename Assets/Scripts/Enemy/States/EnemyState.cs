public enum EnemyStateType { Idle, Patrol, Chase, Attack, Dead }
public abstract class EnemyState
{
    protected readonly Enemy context;
    protected EnemyState(Enemy context) { this.context = context; }
    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public virtual void FixedUpdateState() { }
}