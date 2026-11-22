using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(EnemyPlayerHealth), typeof(GameOverScreen))]
public class Player : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform GroundSphere;

    [Header("Ground Detection")]
    public float FloorDist = 0.2f;
    public LayerMask GroundLayer;

    [Header("Movement")]
    public float WalkSpeed = 10f;

    [Header("Jump")]
    public float JumpForce = 8f;
    [Tooltip("Gravity while rising. Higher values shorten the time to the top of a jump.")]
    [Min(1f)] public float RiseGravityMultiplier = 2f;
    [Tooltip("Gravity while falling. Higher values give a quicker landing.")]
    [Min(1f)] public float FallGravityMultiplier = 3f;

    [Header("Air Movement")]
    public float AirAccel = 10f;
    public float AirBrakeAccel = 20f;
    public float MaxAirSpeed = 5f;

    [Header("Weapon")]
    public Gun CurrentGun;
    public int PlayerAmmo = 10;

    public bool JumpPressed { get; private set; }
    public bool OnFloor { get; private set; }
    public Vector2 MoveInput { get; private set; }

    public PlayerStateMachine StateMachine { get; private set; }

    public EnemyPlayerHealth Health { get; private set; }
    public bool IsDead => Health != null && Health.IsDead;
    private WeaponCommandInvoker weaponCommands;
    private readonly IWeaponCommand startFireCommand = new StartFireCommand();
    private readonly IWeaponCommand stopFireCommand = new StopFireCommand();
    private readonly IWeaponCommand reloadCommand = new ReloadCommand();
    private readonly IWeaponCommand toggleDamageCommand = new ToggleDamageCommand();
    private readonly IWeaponCommand toggleFastReloadCommand = new ToggleFastReloadCommand();

    private void Awake()
    {
        weaponCommands = new WeaponCommandInvoker(this);
        rb = GetComponent<Rigidbody>();
        Health = GetComponent<EnemyPlayerHealth>();
        if (Health == null) Health = gameObject.AddComponent<EnemyPlayerHealth>();
        if (GetComponent<GameOverScreen>() == null) gameObject.AddComponent<GameOverScreen>();

        StateMachine = new PlayerStateMachine(this);
    }

    private void Update()
    {
        HandleUpgradeInput();
        GroundCheck();

        StateMachine.Update();

        // JumpPressed only lasts for one Update frame.
        JumpPressed = false;
    }

    private void HandleUpgradeInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !Application.isFocused || Time.timeScale <= 0f) return;
        if (keyboard.f1Key.wasPressedThisFrame) weaponCommands.Execute(toggleDamageCommand);
        if (keyboard.f2Key.wasPressedThisFrame) weaponCommands.Execute(toggleFastReloadCommand);
    }

    public void RequestToggleDamage()
    {
        if (!IsDead && CurrentGun != null && CurrentGun.isActiveAndEnabled)
            CurrentGun.ToggleDoubleDamage();
    }

    public void RequestToggleFastReload()
    {
        if (!IsDead && CurrentGun != null && CurrentGun.isActiveAndEnabled)
            CurrentGun.ToggleFastReload();
    }

    private void OnGUI()
    {
        if (IsDead || CurrentGun == null || !CurrentGun.isActiveAndEnabled || CurrentGun.EffectiveStats == null)
            return;

        IWeaponStats stats = CurrentGun.EffectiveStats;
        GUI.Box(new Rect(20f, 20f, Mathf.Min(360f, Screen.width - 40f), 80f),
            "WEAPON UPGRADES\n" +
            $"F1  Double damage: {(CurrentGun.DoubleDamage ? "ON" : "OFF")}  ({stats.Damage:0.#} damage)\n" +
            $"F2  Fast reload: {(CurrentGun.FastReload ? "ON" : "OFF")}  ({stats.ReloadDuration:0.##}s)");
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
        ApplyJumpGravity();
    }

    private void ApplyJumpGravity()
    {
        if (IsDead || rb.isKinematic || !rb.useGravity) return;
        if (OnFloor && rb.linearVelocity.y <= 0f) return;

        float multiplier = rb.linearVelocity.y > 0f
            ? RiseGravityMultiplier
            : FallGravityMultiplier;

        // Unity already applies normal gravity. Add only the extra amount.
        rb.AddForce(Physics.gravity * (Mathf.Max(1f, multiplier) - 1f),
            ForceMode.Acceleration);
    }

    public void OnWalk(InputValue value)
    {
        if (IsDead) return;
        MoveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (IsDead) return;
        if (value.isPressed)
        {
            JumpPressed = true;
        }
    }

    private void GroundCheck()
    {
        OnFloor = Physics.CheckSphere(
            GroundSphere.position,
            FloorDist,
            GroundLayer
        );
    }

    public Vector3 GetMoveDirection()
    {
        Vector3 direction =
            transform.right * MoveInput.x +
            transform.forward * MoveInput.y;

        direction.y = 0f;

        return direction.normalized;
    }

    public float ApplyAirControl(float velocity, float input)
    {
        if (Mathf.Approximately(input, 0f))
            return velocity;

        float targetSpeed = input * MaxAirSpeed;

        // At or extremely close to zero.
        if (Mathf.Approximately(velocity, 0f))
        {
            return Mathf.MoveTowards(
                velocity,
                targetSpeed,
                AirAccel * Time.fixedDeltaTime
            );
        }

        bool sameDirection =
            Mathf.Sign(velocity) == Mathf.Sign(input);

        if (sameDirection)
        {
            // Preserve existing momentum if we're already
            // moving faster than MaxAirSpeed.
            if (Mathf.Abs(velocity) >= MaxAirSpeed)
                return velocity;

            return Mathf.MoveTowards(
                velocity,
                targetSpeed,
                AirAccel * Time.fixedDeltaTime
            );
        }

        // Opposite input acts as air braking.
        return Mathf.MoveTowards(
            velocity,
            0f,
            AirBrakeAccel * Time.fixedDeltaTime
        );
    }

    public void OnShoot(InputValue value)
    {
        weaponCommands.Execute(value.isPressed ? startFireCommand : stopFireCommand);
    }

    public void OnShootRelease()
    {
        weaponCommands.Execute(stopFireCommand);
    }

    public void OnReload(InputValue value)
    {
        if (value.isPressed) weaponCommands.Execute(reloadCommand);
    }

    // Receivers keep gameplay checks outside the input callbacks.
    public void RequestStartFire()
    {
        if (IsDead) return;
        if (CurrentGun != null && CurrentGun.isActiveAndEnabled)
        {
            CurrentGun.StartShoot();
        }
        else
        {
            Debug.Log("No Gun Equipped");
        }
    }

    public void RequestStopFire()
    {
        CurrentGun?.StopShoot();
    }
    public void RequestReload()
    {
        if (IsDead) return;
        if (CurrentGun != null && CurrentGun.isActiveAndEnabled)
        {
            Debug.Log("Player Reloading");

            PlayerAmmo = CurrentGun.Reload(PlayerAmmo);
        }
        else
        {
            Debug.Log("No Gun Equipped");
        }
    }
    public void Die()
    {
        if (!IsDead || StateMachine == null) return;
        StateMachine.TryChangeState(StateMachine.CurrentState, PlayerStateType.Dead);
    }

    public void StopForDeath()
    {
        CharacterSounds.Play(this, "Death");
        MoveInput = Vector2.zero;
        JumpPressed = false;
        foreach (Gun gun in GetComponentsInChildren<Gun>(true))
        {
            gun.StopShoot();
            gun.enabled = false;
        }
        if (CurrentGun != null)
        {
            CurrentGun.StopShoot();
            CurrentGun.enabled = false;
        }
        foreach (PlayerLook look in GetComponentsInChildren<PlayerLook>(true))
            look.enabled = false;
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
