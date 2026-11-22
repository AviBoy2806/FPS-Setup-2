using System.Collections.Generic;
public class EnemyStateMachine
{
    private readonly Enemy enemy;
    private readonly Dictionary<EnemyStateType, EnemyState> states;
    private EnemyState currentState;
    public EnemyState CurrentState => currentState;
    public EnemyStateMachine(Enemy enemy)
    {
        this.enemy = enemy;
        states = new Dictionary<EnemyStateType, EnemyState>
        {
            { EnemyStateType.Idle, new EnemyIdleState(enemy) },
            { EnemyStateType.Patrol, new EnemyPatrolState(enemy) },
            { EnemyStateType.Chase, new EnemyChaseState(enemy) },
            { EnemyStateType.Attack, enemy.RangedWeapon != null
                ? (EnemyState)new EnemyRangedAttackState(enemy, enemy.RangedWeapon) : new EnemyAttackState(enemy) },
            { EnemyStateType.Dead, new EnemyDeadState(enemy) }
        };
        Transition(states[EnemyStateType.Idle]);
    }
    public void Update()
    {
        if (enemy.IsDead) Transition(states[EnemyStateType.Dead]);
        currentState?.UpdateState();
    }
    public void FixedUpdate() => currentState?.FixedUpdateState();
    public void TryChangeState(EnemyState caller, EnemyStateType newStateType)
    {
        if (caller != currentState || currentState == states[EnemyStateType.Dead]) return;
        if (enemy.IsDead) newStateType = EnemyStateType.Dead;
        if (states.TryGetValue(newStateType, out EnemyState newState)) Transition(newState);
    }
    private void Transition(EnemyState newState)
    {
        if (newState == currentState) return;
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }
}