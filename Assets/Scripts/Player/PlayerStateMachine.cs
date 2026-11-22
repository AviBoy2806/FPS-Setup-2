using System.Collections.Generic;

public class PlayerStateMachine
{
    private readonly Player player;

    private readonly Dictionary<PlayerStateType, PlayerState> states;

    private PlayerState currentState;

    public PlayerState CurrentState => currentState;

    public PlayerStateMachine(Player player)
    {
        this.player = player;

        states = new Dictionary<PlayerStateType, PlayerState>
        {
            {
                PlayerStateType.Idle,
                new IdleState(player)
            },
            {
                PlayerStateType.Walk,
                new WalkState(player)
            },
            {
                PlayerStateType.Jump,
                new JumpState(player)
            },
            {
                PlayerStateType.Fall,
                new FallState(player)
            },
            {
                PlayerStateType.Dead, new PlayerDeadState(player)
            }
        };

        Transition(states[PlayerStateType.Idle]);
    }

    public void Update()
    {
        if (player.IsDead) Transition(states[PlayerStateType.Dead]);
        currentState?.UpdateState();
    }

    public void FixedUpdate()
    {
        if (player.IsDead) Transition(states[PlayerStateType.Dead]);
        currentState?.FixedUpdateState();
    }

    public void TryChangeState(
        PlayerState caller,
        PlayerStateType newStateType
    )
    {
        // Prevent an old state from changing the state machine
        // after another transition has already happened.
        if (caller != currentState || currentState == states[PlayerStateType.Dead])
            return;

        if (player.IsDead) newStateType = PlayerStateType.Dead;
        if (states.TryGetValue(newStateType, out PlayerState newState))
        {
            Transition(newState);
        }
    }

    private void Transition(PlayerState newState)
    {
        if (newState == currentState)
            return;

        currentState?.ExitState();

        currentState = newState;

        currentState.EnterState();
    }
}