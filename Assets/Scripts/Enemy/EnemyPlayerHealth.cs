using UnityEngine;
using UnityEngine.Events;
[DisallowMultipleComponent]
public class EnemyPlayerHealth : MonoBehaviour
{
    [Min(1f)] public float MaxHealth = 100f;
    public UnityEvent OnDeath = new UnityEvent();
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    private void Awake()
    {
        CurrentHealth = MaxHealth;
        if (GetComponent<HealthBarDisplay>() == null) gameObject.AddComponent<HealthBarDisplay>();
    }
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f) return;
        PlayerDamageVignette flash = GetComponent<PlayerDamageVignette>();
        if (flash == null) flash = gameObject.AddComponent<PlayerDamageVignette>();
        flash.Flash();
        CharacterSounds.Play(this, "Hurt");
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Debug.Log($"Player health: {CurrentHealth}", this);
        if (IsDead)
        {
            GetComponent<Player>()?.Die();
            // Publisher: announce death. Listeners decide how to respond.
            OnDeath.Invoke();
        }
    }
}
