using UnityEngine;

public class WalkState : PlayerState
{
    public WalkState(Player context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Walk State Entered!");
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
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

        if (context.MoveInput == Vector2.zero)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Idle
            );

            return;
        }
    }

    public override void FixedUpdateState()
    {
        Vector3 velocity = context.rb.linearVelocity;
        Vector3 moveDirection = context.GetMoveDirection();

        velocity.x = moveDirection.x * context.WalkSpeed;
        velocity.z = moveDirection.z * context.WalkSpeed;

        context.rb.linearVelocity = velocity;
    }
}