using UnityEngine;

public class JumpState : PlayerState
{
    private bool jumpPending;

    public JumpState(Player context) : base(context)
    {
    }

    public override void EnterState()
    {
        if (!context.OnFloor)
        {
            Debug.Log("Cannot jump: not on ground.");

            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Fall
            );

            return;
        }

        Debug.Log("Jump State Entered!");

        CharacterSounds.Play(context, "Jump");
        jumpPending = true;
    }

    public override void ExitState()
    {
        jumpPending = false;
    }

    public override void UpdateState()
    {
        // Wait until FixedUpdate actually applies
        // the jump impulse.
        if (jumpPending)
            return;

        if (context.rb.linearVelocity.y <= 0f)
        {
            context.StateMachine.TryChangeState(
                this,
                PlayerStateType.Fall
            );

            return;
        }
    }

    public override void FixedUpdateState()
    {
        if (jumpPending)
        {
            context.rb.AddForce(
                Vector3.up * context.JumpForce,
                ForceMode.Impulse
            );

            jumpPending = false;
        }

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