using UnityEngine;

[DisallowMultipleComponent]
public class CharacterSounds : MonoBehaviour
{
    public AudioClip Hurt, Death, Footstep, Jump, Land, Shoot, Reload, AttackCue, MeleeAttack;
    [Range(0f, 1f)] public float Volume = 0.8f;
    [Min(0.1f)] public float StepInterval = 0.45f;
    [Min(1f)] public float HearingDistance = 25f;
    private AudioSource source;
    private AudioSource footstepSource;
    private AudioSource shootingSource;
    private Player player;
    private Enemy enemy;
    private float nextStep;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        enemy = GetComponentInParent<Enemy>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = enemy != null ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = HearingDistance;
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = source.spatialBlend;
        footstepSource.rolloffMode = source.rolloffMode;
        footstepSource.minDistance = source.minDistance;
        footstepSource.maxDistance = source.maxDistance;
        shootingSource = gameObject.AddComponent<AudioSource>();
        shootingSource.playOnAwake = false;
        shootingSource.spatialBlend = source.spatialBlend;
        shootingSource.rolloffMode = source.rolloffMode;
        shootingSource.minDistance = source.minDistance;
        shootingSource.maxDistance = source.maxDistance;
    }
    public static void Play(Component actor, string action)
    {
        CharacterSounds sounds = actor.GetComponentInParent<CharacterSounds>();
        if (sounds == null) sounds = actor.GetComponentInChildren<CharacterSounds>();
        if (sounds == null)
        {
            Player owner = actor.GetComponentInParent<Player>();
            if (owner != null) sounds = owner.GetComponentInChildren<CharacterSounds>();
        }
        if (sounds != null) sounds.Play(action);
    }
    public static void StopShooting(Component actor)
    {
        CharacterSounds sounds = actor.GetComponentInParent<CharacterSounds>();
        if (sounds == null) sounds = actor.GetComponentInChildren<CharacterSounds>();
        if (sounds == null)
        {
            Player owner = actor.GetComponentInParent<Player>();
            if (owner != null) sounds = owner.GetComponentInChildren<CharacterSounds>();
        }
        if (sounds != null && sounds.shootingSource != null) sounds.shootingSource.Stop();
    }

    public void Play(string action)
    {
        if (action == "Shoot")
        {
            if (Shoot != null && shootingSource != null)
            {
                shootingSource.clip = Shoot;
                shootingSource.volume = Volume;
                if (!shootingSource.isPlaying) shootingSource.Play();
            }
            return;
        }
        if (action == "Death") StopShooting(this);
        AudioClip clip = null;
        switch (action)
        {
            case "Hurt": clip = Hurt; break;
            case "Death": clip = Death; break;
            case "Footstep": clip = Footstep; break;
            case "Jump": clip = Jump; break;
            case "Land": clip = Land; break;
            case "Shoot": clip = Shoot; break;
            case "Reload": clip = Reload; break;
            case "AttackCue": clip = AttackCue; break;
            case "MeleeAttack": clip = MeleeAttack; break;
        }
        if (clip != null && source != null) source.PlayOneShot(clip, Volume);
    }
    private void LateUpdate()
    {
        bool moving = player != null && !player.IsDead && player.OnFloor && player.MoveInput.sqrMagnitude > 0.01f &&
            player.rb != null && new Vector2(player.rb.linearVelocity.x, player.rb.linearVelocity.z).sqrMagnitude > 0.1f;
        if (enemy != null) moving = !enemy.IsDead && enemy.CanNavigate && enemy.Agent.velocity.sqrMagnitude > 0.1f;
        if (!moving)
        {
            if (footstepSource.isPlaying) footstepSource.Stop();
            nextStep = Time.time;
            return;
        }
        if (Footstep == null || footstepSource.isPlaying || Time.time < nextStep) return;
        nextStep = Time.time + Mathf.Max(0.1f, StepInterval);
        footstepSource.clip = Footstep;
        footstepSource.volume = Volume;
        footstepSource.Play();
    }
    private void OnDisable()
    {
        if (footstepSource != null) footstepSource.Stop();
        if (shootingSource != null) shootingSource.Stop();
    }
}