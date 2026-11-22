using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only checks. They use a temporary scene and do not save changes.
public static class WeaponPatternChecks
{
    [MenuItem("Tools/Check Weapon Patterns")]
    public static void Run()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            CheckStats();
            Player player = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"))
                .GetComponent<Player>();
            player.transform.position = new Vector3(10000f, 10000f, 10000f);
            foreach (Collider collider in player.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (CharacterSounds sounds in player.GetComponentsInChildren<CharacterSounds>())
                Initialize(sounds);
            Initialize(player.GetComponent<EnemyPlayerHealth>());
            Initialize(player);
            Gun gun = player.CurrentGun;
            Initialize(gun);
            Equal(gun.EffectiveStats.Damage, 25f, "Migrated damage");
            Equal(gun.EffectiveStats.FireRate, 10f, "Existing fire rate");
            Equal(gun.EffectiveStats.Range, 100f, "Existing range");
            Equal(gun.EffectiveStats.ReloadDuration, 1f, "Existing reload time");
            Require(gun.Capacity == 100 && !gun.DoubleDamage && !gun.FastReload,
                "Original magazine and upgrades off by default");

            CheckCommands(player, gun);
            CheckShots(player, gun);
            Debug.Log("WEAPON PATTERN CHECKS PASSED: stats, commands, reload, damage, walls, and prefab references.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void CheckStats()
    {
        for (int mask = 0; mask < 4; mask++)
        {
            IWeaponStats stats = new BaseWeaponStats(25f, 10f, 100f, 1f);
            if ((mask & 1) != 0) stats = new DamageBoostDecorator(stats);
            if ((mask & 2) != 0) stats = new FastReloadDecorator(stats);
            Equal(stats.Damage, (mask & 1) != 0 ? 50f : 25f, "Upgrade damage");
            Equal(stats.ReloadDuration, (mask & 2) != 0 ? 0.5f : 1f, "Upgrade reload");
            Equal(stats.FireRate, 10f, "Fire rate passes through wrappers");
            Equal(stats.Range, 100f, "Range passes through wrappers");
        }
        IWeaponStats invalid = new BaseWeaponStats(-1f, 0f, -1f, -1f);
        Require(invalid.FireRate > 0f && invalid.Damage == 0f &&
            invalid.Range == 0f && invalid.ReloadDuration == 0f, "Safe base values");
    }

    private static void CheckCommands(Player player, Gun gun)
    {
        var invoker = new WeaponCommandInvoker(player);
        for (int i = 0; i < 3; i++)
        {
            invoker.Execute(new ToggleDamageCommand());
            invoker.Execute(new ToggleFastReloadCommand());
            Equal(gun.EffectiveStats.Damage, 50f, "Runtime damage toggle does not stack");
            Equal(gun.EffectiveStats.ReloadDuration, 0.5f, "Runtime reload toggle");
            invoker.Execute(new ToggleDamageCommand());
            Equal(gun.EffectiveStats.Damage, 25f, "Damage toggle restores base");
            Equal(gun.EffectiveStats.ReloadDuration, 0.5f, "Other upgrade stays enabled");
            invoker.Execute(new ToggleFastReloadCommand());
            Equal(gun.EffectiveStats.ReloadDuration, 1f, "Reload toggle restores base");
        }
        invoker.Execute(new StartFireCommand());
        Require(gun.TryShoot, "Start command");
        invoker.Execute(new StopFireCommand());
        Require(!gun.TryShoot, "Release command");
        gun.CurrentAmmo = 80;
        player.PlayerAmmo = 10;
        invoker.Execute(new ReloadCommand());
        Require(gun.CurrentAmmo == 90 && player.PlayerAmmo == 0 && gun.isReloading,
            "Reload transfers only available ammo");
        invoker.Execute(new ReloadCommand());
        Require(gun.CurrentAmmo == 90 && player.PlayerAmmo == 0, "Repeated reload is ignored");
        gun.isReloading = false;
        invoker.Execute(new ReloadCommand());
        Require(!gun.isReloading, "No reserve ammo");
        gun.CurrentAmmo = gun.Capacity;
        player.PlayerAmmo = 10;
        invoker.Execute(new ReloadCommand());
        Require(!gun.isReloading && player.PlayerAmmo == 10, "Full magazine");
        gun.enabled = false;
        invoker.Execute(new StartFireCommand());
        invoker.Execute(new ReloadCommand());
        Require(!gun.TryShoot && !gun.isReloading, "Disabled gun rejects actions");
        gun.enabled = true;
        player.CurrentGun = null;
        invoker.Execute(new StartFireCommand());
        invoker.Execute(new StopFireCommand());
        invoker.Execute(new ReloadCommand());
        player.CurrentGun = gun;

        EnemyPlayerHealth health = player.Health;
        typeof(EnemyPlayerHealth).GetProperty("CurrentHealth").GetSetMethod(true)
            .Invoke(health, new object[] { 0f });
        invoker.Execute(new StartFireCommand());
        invoker.Execute(new ReloadCommand());
        Require(!gun.TryShoot && !gun.isReloading, "Dead player rejects actions");
        gun.TryShoot = true;
        invoker.Execute(new StopFireCommand());
        Require(!gun.TryShoot, "Release still works after death");
        Initialize(health);
    }

    private static void CheckShots(Player player, Gun gun)
    {
        Transform view = gun.playerCamera.transform;
        var target = new GameObject("Test enemy");
        target.transform.position = view.position + view.forward * 5f;
        Enemy enemy = target.AddComponent<Enemy>();
        Initialize(enemy);
        EnemyHitFlash flash = target.AddComponent<EnemyHitFlash>();
        Initialize(flash);
        var hitbox = new GameObject("Child hitbox");
        hitbox.transform.SetParent(target.transform, false);
        hitbox.layer = 8;
        hitbox.AddComponent<BoxCollider>();
        Physics.SyncTransforms();

        gun.CurrentAmmo = 10;
        gun.TryShoot = true;
        gun.NextShootTime = Time.time - 1f;
        var shooting = new ShootingState(gun);
        shooting.HandleShoot();
        Equal(enemy.Health.Health, 75f, "One hit, one damage application, parent lookup");
        Require(gun.CurrentAmmo == 9, "One round consumed");
        shooting.HandleShoot();
        Require(gun.CurrentAmmo == 9, "Cooldown blocks extra shot");

        var wall = new GameObject("Test wall");
        wall.transform.position = view.position + view.forward * 2f;
        wall.AddComponent<BoxCollider>();
        Physics.SyncTransforms();
        gun.NextShootTime = Time.time - 1f;
        shooting.HandleShoot();
        Equal(enemy.Health.Health, 75f, "Wall blocks damage");
        Require(gun.CurrentAmmo == 8, "Blocked shot still consumes ammo");

        UnityEngine.Object.DestroyImmediate(wall);
        gun.DoubleDamage = true;
        Initialize(gun);
        gun.TryShoot = true;
        gun.NextShootTime = Time.time - 1f;
        Physics.SyncTransforms();
        shooting.HandleShoot();
        Equal(enemy.Health.Health, 25f, "Damage upgrade reaches the actual shot");

        gun.CurrentAmmo = 0;
        gun.NextShootTime = Time.time - 1f;
        shooting.HandleShoot();
        Equal(enemy.Health.Health, 25f, "Empty magazine cannot damage");
        gun.CurrentAmmo = 10;
        gun.isReloading = true;
        shooting.HandleShoot();
        Require(gun.CurrentAmmo == 10, "Cannot fire while reloading");
        gun.isReloading = false;
        gun.TryShoot = false;
        shooting.HandleShoot();
        Require(gun.CurrentAmmo == 10, "Cannot fire after release");

        gun.FastReload = true;
        Initialize(gun);
        var reload = new ReloadingState(gun);
        reload.EnterState();
        float finish = (float)typeof(ReloadingState).GetField("ReloadFinishTime",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reload);
        float animationDuration = (float)typeof(GunAnimation).GetField("reloadDuration",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gun.Animation);
        Equal(finish - Time.time, 0.5f, "Decorated reload timer");
        Equal(animationDuration, 0.5f, "Matching reload animation duration");
        reload.ExitState();
    }

    private static void Initialize(MonoBehaviour component)
    {
        component.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(component, null);
    }

    private static void Equal(float actual, float expected, string message)
    {
        Require(Mathf.Abs(actual - expected) < 0.001f, message + ": " + actual);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Weapon check failed: " + message);
    }
}
