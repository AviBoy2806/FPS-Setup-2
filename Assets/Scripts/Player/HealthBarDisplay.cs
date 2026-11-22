using UnityEngine;

// Automatic health display for the player HUD and visible enemies.
[DisallowMultipleComponent]
public class HealthBarDisplay : MonoBehaviour
{
    private EnemyPlayerHealth playerHealth;
    private Enemy enemy;
    private float enemyMaxHealth;
    private Camera viewer;

    private void Start()
    {
        playerHealth = GetComponent<EnemyPlayerHealth>();
        enemy = GetComponent<Enemy>();
        if (enemy != null) enemyMaxHealth = Mathf.Max(1f, enemy.Health.Health);
        Player player = FindAnyObjectByType<Player>();
        if (player != null) viewer = player.GetComponentInChildren<Camera>();
        if (viewer == null) viewer = Camera.main;
    }

    private void OnGUI()
    {
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        GUI.depth = 20; // Behind damage feedback and Game Over.
        if (playerHealth != null)
        {
            float width = Mathf.Min(260f, Screen.width - 40f);
            Draw(new Rect(20f, Screen.height - 62f, width, 28f),
                playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else if (enemy != null && !enemy.IsDead && viewer != null)
        {
            Vector3 head = transform.position + Vector3.up * 1.4f;
            Vector3 screen = viewer.WorldToScreenPoint(head);
            if (screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                screen.y >= 0f && screen.y <= Screen.height &&
                !Physics.Linecast(viewer.transform.position, head, enemy.ObstacleLayers,
                    QueryTriggerInteraction.Ignore))
                Draw(new Rect(screen.x - 50f, Screen.height - screen.y, 100f, 16f),
                    enemy.Health.Health, enemyMaxHealth);
        }
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private static void Draw(Rect rect, float current, float maximum)
    {
        float ratio = Mathf.Clamp01(current / Mathf.Max(1f, maximum));
        GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(0.15f, 0.85f, 0.3f), ratio);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f,
            (rect.width - 4f) * ratio, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUIStyle text = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = rect.height > 20f ? 16 : 11
        };
        GUI.Label(rect, Mathf.CeilToInt(Mathf.Max(0f, current)) + " / " +
            Mathf.CeilToInt(maximum), text);
    }
}
