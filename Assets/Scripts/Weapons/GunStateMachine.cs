using System.Collections.Generic;

public class GunStateMachine
{
    private readonly Gun Gun;

    private readonly Dictionary<GunStateType, GunState> states;

    private GunState currentState;

    public GunState CurrentState => currentState;

    public GunStateMachine(Gun Gun)
    {
        this.Gun = Gun;

        states = new Dictionary<GunStateType, GunState>
        {
            {
                GunStateType.Idle,
                new GunIdleState(Gun)
            },
            {
                GunStateType.Shooting,
                new ShootingState(Gun)
            },
            {
                GunStateType.Reloading,
                new ReloadingState(Gun)
            }
        };

        Transition(states[GunStateType.Idle]);
    }

    public void Update()
    {
        currentState?.UpdateState();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdateState();
    }

    public void TryChangeState(
        GunState caller,
        GunStateType newStateType
    )
    {
        // Prevent an old state from changing the state machine
        // after another transition has already happened.
        if (caller != currentState)
            return;

        if (states.TryGetValue(newStateType, out GunState newState))
        {
            Transition(newState);
        }
    }

    private void Transition(GunState newState)
    {
        if (newState == currentState)
            return;

        currentState?.ExitState();

        currentState = newState;

        currentState.EnterState();
    }
}