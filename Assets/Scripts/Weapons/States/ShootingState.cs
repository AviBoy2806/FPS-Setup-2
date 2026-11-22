using UnityEngine;

public class ShootingState : GunState
{
    public ShootingState(Gun context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("Gun Shooting");
    }

    public override void ExitState()
    { CharacterSounds.StopShooting(context);
    }

    public override void UpdateState()
    {
        if (context.isReloading)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Reloading);
            return;
        }
        if (!context.TryShoot)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Idle);
            return;
        }
        if(context.CurrentAmmo <= 0)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Idle);
            return;
        }
        if(Time.time >= context.NextShootTime)
        {
            HandleShoot();
        }

        if (!context.TryShoot)
        {
            context.gunStateMachine.TryChangeState(this, GunStateType.Idle);
            return;
        }
    }

    public override void FixedUpdateState()
    {
    }

    public void HandleShoot()
    {
        if (Time.time < context.NextShootTime || context.CurrentAmmo <= 0 ||
            context.isReloading || !context.TryShoot || context.playerCamera == null)
            return;

        Ray ray = new Ray(
            context.playerCamera.transform.position,
            context.playerCamera.transform.forward
        );

        // One ray finds the first solid object, including walls.
        if (Physics.Raycast(ray, out RaycastHit hit, context.EffectiveStats.Range,
            Physics.DefaultRaycastLayers | context.DamageLayers.value, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit: " + hit.collider.name);
            if ((context.DamageLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null) enemy.TakeDamage(context.EffectiveStats.Damage);
            }
        }
        CharacterSounds.Play(context, "Shoot");
        context.CurrentAmmo--;
        if (context.CurrentAmmo <= 0) CharacterSounds.StopShooting(context);
        context.Animation?.PlayShot();
        context.NextShootTime = Time.time + (1f / context.EffectiveStats.FireRate);
    }
}
