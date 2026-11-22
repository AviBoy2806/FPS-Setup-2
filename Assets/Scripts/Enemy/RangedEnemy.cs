using UnityEngine;

// Add beside Enemy to replace melee attacks with a ranged weapon.
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class RangedEnemy : MonoBehaviour
{
    public Transform Muzzle;
    [Min(1f)] public float ShootingRange = 12f;
    [Min(0f)] public float PreferredDistance = 8f;
    [Min(0f)] public float RetreatDistance = 4f;
    [Min(0f)] public float Damage = 8f;
    [Min(0.05f)] public float ShotInterval = 0.8f;
    [Min(0f)] public float AimDelay = 0.5f;
    [Range(0f, 15f)] public float SpreadDegrees = 2f;
    [Tooltip("Include player and environment layers. Walls must remain included.")]
    public LayerMask HitLayers = Physics.DefaultRaycastLayers;
    [Tooltip("Solid layers that block the barrel and bullets. Keep walls included, even if Hit Layers only contains Player.")]
    public LayerMask ShotObstacleLayers = Physics.DefaultRaycastLayers;
    [Min(0.001f)] public float MuzzleClearance = 0.04f;
    public ParticleSystem MuzzleFlash;
    public AudioSource ShotAudio;
    public bool ShowTracer = true;

    [Header("Attack rhythm")]
    [Min(0.05f)] public float AimTrackingDelay = 0.35f;
    [Min(0.1f)] public float BurstPause = 1.8f;
    [Min(0.1f)] public float CueDuration = 0.65f;
    public bool ShowAttackCue = true;

    private struct AimSample { public float Time; public Vector3 Position; }
    private readonly System.Collections.Generic.Queue<AimSample> aimHistory =
        new System.Collections.Generic.Queue<AimSample>();
    private Vector3 delayedAim;
    private int shotsRemaining;
    private float nextBurstTime;
    private float cueFinishTime;
    private bool active;
    private bool cueActive;
    private Camera viewer;

    public void BeginAttack()
    {
        active = true;
        cueActive = false;
        shotsRemaining = 0;
        aimHistory.Clear();
        delayedAim = enemy.Target.position + Vector3.up * 0.5f;
        viewer = enemy.Target.GetComponentInChildren<Camera>();
        if (viewer == null) viewer = Camera.main;
    }

    private void StopShotAudio()
    {
        CharacterSounds.StopShooting(this);
        if (ShotAudio != null) ShotAudio.Stop();
    }
    private void Update()
    {
        if (active && shotsRemaining == 0 && Time.time >= nextShotTime) StopShotAudio();
    }

    public void CancelAttack()
    {
        StopShotAudio();
        active = false;
        cueActive = false;
        shotsRemaining = 0;
        aimHistory.Clear();
    }

    public void TickAttack(bool facingTarget)
    {
        if (!active || !enabled || enemy.IsDead || !enemy.HasTarget) return;
        aimHistory.Enqueue(new AimSample { Time = Time.time,
            Position = enemy.Target.position + Vector3.up * 0.5f });
        while (aimHistory.Count > 0 &&
            aimHistory.Peek().Time <= Time.time - Mathf.Clamp(AimTrackingDelay, 0.05f, 2f))
            delayedAim = aimHistory.Dequeue().Position;

        if (!HasClearShot())
        { StopShotAudio();
            cueActive = false;
            shotsRemaining = 0;
            return;
        }
        if (shotsRemaining == 0 && !cueActive)
        {
            if (Time.time < nextBurstTime || !facingTarget) return;
            CharacterSounds.Play(this, "AttackCue");
            cueActive = true;
            cueFinishTime = Time.time + Mathf.Max(0.1f, CueDuration, AimDelay, AimTrackingDelay);
        }
        if (cueActive)
        {
            if (Time.time < cueFinishTime || !facingTarget) return;
            cueActive = false;
            shotsRemaining = Random.Range(2, 4);
        }
        if (!facingTarget) { StopShotAudio(); return; }
        if (!TryFire()) return;
        shotsRemaining--;
        if (shotsRemaining == 0)
            nextBurstTime = Time.time + Mathf.Max(0.1f, BurstPause);
    }

    private void OnDisable() => CancelAttack();

    private void OnGUI()
    {
        if (!ShowAttackCue || !cueActive || !active || enemy.IsDead || viewer == null) return;
        Vector3 screen = viewer.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screen.z <= 0f) return;
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 0.65f, 0.05f);
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(screen.x - 30f, Screen.height - screen.y - 30f, 60f, 60f), "!", style);
        GUI.color = oldColor;
    }

    private Enemy enemy;
    private Material tracerMaterial;
    private float nextShotTime;

    private void Awake() => enemy = GetComponent<Enemy>();

    private bool TryFire()
    {
        if (!enabled || enemy.IsDead || !enemy.HasTarget || !enemy.CanSeeTarget() ||
            enemy.TargetDistance > ShootingRange || Time.time < nextShotTime) return false;
        if (!HasClearShot()) { StopShotAudio(); return false; }
        Vector3 eye = enemy.Eye != null ? enemy.Eye.position : transform.position + Vector3.up * 0.5f;
        Vector3 origin = Muzzle != null ? Muzzle.position : eye;
        // A weapon poking through a wall must not fire from the other side.
        if (SegmentBlocked(eye, origin) || SegmentBlocked(origin, delayedAim)) return false;
        Vector3 aim = delayedAim - origin;
        if (aim.sqrMagnitude < 0.001f) return false;
        nextShotTime = Time.time + Mathf.Max(0.05f, ShotInterval);
        Vector2 spread = Random.insideUnitCircle * SpreadDegrees;
        Vector3 direction = Quaternion.LookRotation(aim) * Quaternion.Euler(spread.y, spread.x, 0f) * Vector3.forward;
        Vector3 end = origin + direction * ShootingRange;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, ShootingRange,
            HitLayers.value | ShotObstacleLayers.value | enemy.ObstacleLayers.value, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            // Skip the enemy's own body and weapon colliders.
            if (hit.collider.transform.IsChildOf(transform)) continue;
            end = hit.point;
            EnemyPlayerHealth health = hit.collider.GetComponentInParent<EnemyPlayerHealth>();
            if (health != null && health.transform == enemy.Target)
                health.TakeDamage(Damage);
            break;
        }
        if (MuzzleFlash != null) MuzzleFlash.Play();
        if (ShotAudio != null && ShotAudio.clip != null) { if (!ShotAudio.isPlaying) ShotAudio.Play(); }
        if (ShotAudio == null || ShotAudio.clip == null) CharacterSounds.Play(this, "Shoot");
        if (ShowTracer) DrawTracer(origin, end);
        return true;
    }

    public bool HasClearShot()
    {
        if (enemy == null || !enemy.HasTarget) return false;
        Vector3 eye = enemy.Eye != null ? enemy.Eye.position : transform.position + Vector3.up * 0.5f;
        Vector3 origin = Muzzle != null ? Muzzle.position : eye;
        int mask = ShotObstacleLayers.value | enemy.ObstacleLayers.value | HitLayers.value;
        // Raycasts do not detect a collider containing the origin.
        foreach (Collider collider in Physics.OverlapSphere(origin, Mathf.Max(0.001f, MuzzleClearance),
            mask, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(transform) ||
                collider.transform.IsChildOf(enemy.Target)) continue;
            return false;
        }
        return !SegmentBlocked(eye, origin) &&
            !SegmentBlocked(origin, enemy.Target.position + Vector3.up * 0.5f);
    }

    private bool SegmentBlocked(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < 0.000001f) return false;
        int mask = ShotObstacleLayers.value | enemy.ObstacleLayers.value | HitLayers.value;
        RaycastHit[] hits = Physics.RaycastAll(from, delta.normalized, delta.magnitude,
            mask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            // The target is the first solid object on this segment.
            if (hit.collider.transform.IsChildOf(enemy.Target)) return false;
            return true;
        }
        return false;
    }
    private void DrawTracer(Vector3 origin, Vector3 end)
    {
        if (tracerMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            tracerMaterial = new Material(shader);
            tracerMaterial.color = new Color(1f, 0.65f, 0.15f);
        }
        GameObject tracer = new GameObject("Enemy bullet tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.sharedMaterial = tracerMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
        line.startWidth = 0.025f;
        line.endWidth = 0.01f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Destroy(tracer, 0.07f);
    }

    private void OnDestroy()
    {
        if (tracerMaterial != null) Destroy(tracerMaterial);
    }
}
