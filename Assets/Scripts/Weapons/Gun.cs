using UnityEngine;

public class Gun : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 100f;
    public int FireRate = 5;
    public int Capacity = 100;
    [SerializeField]
    private int currentAmmo = 100;
    public bool TryShoot = false;
    public bool isReloading = false;
    public float ReloadTime = 1.0f;
    [Min(0f)] public float Damage = 25f;
    [Tooltip("Layers that can receive damage. Walls still block the shot.")]
    public LayerMask DamageLayers = Physics.DefaultRaycastLayers;

    [Header("Starting upgrades (F1/F2 toggle during Play)")]
    public bool DoubleDamage;
    public bool FastReload;

    public IWeaponStats EffectiveStats { get; private set; }
    public GunStateMachine gunStateMachine {get; private set;}
    public float NextShootTime = 0;


    public GunAnimation Animation { get; private set; }
    void Awake()
    {
        RebuildStats();

        Animation = GetComponent<GunAnimation>();
        if (Animation == null) Animation = gameObject.AddComponent<GunAnimation>();
        gunStateMachine = new GunStateMachine(this);
    }

    public void ToggleDoubleDamage()
    {
        DoubleDamage = !DoubleDamage;
        RebuildStats();
    }

    public void ToggleFastReload()
    {
        FastReload = !FastReload;
        RebuildStats();
    }

    private void RebuildStats()
    {
        // Always start from base values so repeated toggles never stack upgrades.
        EffectiveStats = new BaseWeaponStats(Damage, FireRate, range, ReloadTime);
        if (DoubleDamage) EffectiveStats = new DamageBoostDecorator(EffectiveStats);
        if (FastReload) EffectiveStats = new FastReloadDecorator(EffectiveStats);
    }

    void Update()
    {
        gunStateMachine.Update();
    }
    void FixedUpdate()
    {
        gunStateMachine.FixedUpdate();
    }
    public int CurrentAmmo
    {
        get=>currentAmmo;
        set=>currentAmmo = Mathf.Clamp(value, 0, Capacity);
    }

    public void StartShoot()
    {
        //Debug.Log("Pew");
        TryShoot = true;
    }   
    public void StopShoot()
    {
        //Debug.Log("Stopped Shooting");
        TryShoot = false;
        CharacterSounds.StopShooting(this);
    }
    
    public int Reload(int Ammo)
    {
        if(isReloading || CurrentAmmo >= Capacity || Ammo <= 0)
        {
            Debug.Log("Gun is Fully Loaded/You have no Ammo");
            return Ammo;
        } 
        Debug.Log("Reloading");
        isReloading = true;
        int loaded = Mathf.Min(Ammo, Capacity - CurrentAmmo);
        CurrentAmmo += loaded;
        return Ammo - loaded;
    }
    private void OnDisable()
    {
        StopShoot();
        Animation?.Cancel();
    }
}
