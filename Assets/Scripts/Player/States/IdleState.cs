using UnityEngine;

public class IdleState : PlayerState
{
    public IdleState(Player context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Idle State Entered!");
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        // Check jump first so jumping isn't delayed
        // by another state transition.
        if (context.JumpPressed)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Jump
            );

            return;
        }

        if (!context.OnFloor &&
            context.rb.linearVelocity.y < 0f)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Fall
            );

            return;
        }

        if (context.MoveInput != Vector2.zero)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Walk
            );

            return;
        }
    }

    public override void FixedUpdateState()
    {
        Vector3 velocity = context.rb.linearVelocity;

        velocity.x = 0f;
        velocity.z = 0f;

        context.rb.linearVelocity = velocity;
    }
}