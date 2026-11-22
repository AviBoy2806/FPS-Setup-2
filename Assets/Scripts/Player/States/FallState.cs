using UnityEngine;

public class FallState : PlayerState
{
    public FallState(Player context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Fall State Entered!");
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        if (!context.OnFloor)
            return;

        CharacterSounds.Play(context, "Land");
        if (context.MoveInput != Vector2.zero)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Walk
            );
        }
        else
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Idle
            );
        }
    }

    public override void FixedUpdateState()
    {
        Vector3 velocity = context.rb.linearVelocity;
        Vector3 moveDirection = context.GetMoveDirection();

        velocity.x = context.ApplyAirControl(
            velocity.x,
            moveDirection.x
        );

        velocity.z = context.ApplyAirControl(
            velocity.z,
            moveDirection.z
        );

        context.rb.linearVelocity = velocity;
    }
}