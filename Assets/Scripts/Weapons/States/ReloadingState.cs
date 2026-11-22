using UnityEngine;

public class ReloadingState : GunState
{
    private float ReloadFinishTime = 0.0f;
    public ReloadingState(Gun context) : base(context)
    {
    }

    public override void EnterState()
    {
        CharacterSounds.Play(context, "Reload");
        Debug.Log("Gun Reloading");
        float duration = context.EffectiveStats.ReloadDuration;
        ReloadFinishTime = Time.time + duration;
        context.Animation?.BeginReload(duration);
    }

    public override void ExitState()
    {
        context.isReloading = false;
        context.Animation?.EndReload();
    }

    public override void UpdateState()
    {
        if(Time.time >= ReloadFinishTime)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Idle);
        }
    }

    public override void FixedUpdateState()
    {
    }
}
