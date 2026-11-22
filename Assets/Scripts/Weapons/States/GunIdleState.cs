using UnityEngine;

public class GunIdleState : GunState
{
    public GunIdleState(Gun context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Gun Idle");
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        if (context.TryShoot && context.CurrentAmmo > 0 && !context.isReloading)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Shooting);
            return;
        }
        if (context.isReloading)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Reloading);
            return;
        }
    }

    public override void FixedUpdateState()
    {
    }
}