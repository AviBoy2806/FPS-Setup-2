using UnityEngine;

// The base stats and every wrapper share this interface.
public interface IWeaponStats
{
    float Damage { get; }
    float FireRate { get; }
    float Range { get; }
    float ReloadDuration { get; }
}

public sealed class BaseWeaponStats : IWeaponStats
{
    public float Damage { get; }
    public float FireRate { get; }
    public float Range { get; }
    public float ReloadDuration { get; }

    public BaseWeaponStats(float damage, float fireRate, float range, float reloadDuration)
    {
        Damage = Mathf.Max(0f, damage);
        FireRate = Mathf.Max(0.01f, fireRate);
        
        Range = Mathf.Max(0f, range);
        ReloadDuration = Mathf.Max(0f, reloadDuration);
    }
}

// A decorator wraps another stats object and passes through unchanged values.
public abstract class WeaponStatsDecorator : IWeaponStats
{
    protected readonly IWeaponStats wrapped;

    protected WeaponStatsDecorator(IWeaponStats wrapped)
    {
        this.wrapped = wrapped;
    }

    public virtual float Damage => wrapped.Damage;
    public virtual float FireRate => wrapped.FireRate;
    public virtual float Range => wrapped.Range;
    public virtual float ReloadDuration => wrapped.ReloadDuration;
}

public sealed class DamageBoostDecorator : WeaponStatsDecorator
{
    public DamageBoostDecorator(IWeaponStats wrapped) : base(wrapped) { }

    public override float Damage => wrapped.Damage * 2f;
}

public sealed class FastReloadDecorator : WeaponStatsDecorator
{
    public FastReloadDecorator(IWeaponStats wrapped) : base(wrapped) { }

    public override float ReloadDuration => wrapped.ReloadDuration * 0.5f;
}
