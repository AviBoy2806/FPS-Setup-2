using UnityEngine;
using UnityEngine.SceneManagement;

// Self-contained overlay; requires no Canvas or Inspector event wiring.
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyPlayerHealth))]
public class GameOverScreen : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private bool restarting;
    private EnemyPlayerHealth health;

    private void OnEnable()
    {
        // Observer: sign up for the health component's death announcement.
        health = GetComponent<EnemyPlayerHealth>();
        health.OnDeath.AddListener(Show);
    }

    private void OnDisable()
    {
        // Stop listening while this screen is disabled or being destroyed.
        if (health != null) health.OnDeath.RemoveListener(Show);
    }

    public void Show()
    {
        IsShowing = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (!IsShowing) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (!IsShowing) return;
        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };
        float centerY = Screen.height * 0.5f;
        GUI.Label(new Rect(0, centerY - 100, Screen.width, 70), "GAME OVER", title);
        GUIStyle message = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, centerY - 30, Screen.width, 30), "You died.", message);
        GUI.enabled = !restarting;
        if (GUI.Button(new Rect(Screen.width * 0.5f - 100, centerY + 20, 200, 45), "Retry"))
            Retry();
        GUI.enabled = previousEnabled;
        GUI.color = previousColor;
    }

    public void Retry()
    {
        if (restarting) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex < 0)
        {
            Debug.LogError("Add the current scene to Build Profiles > Scene List to enable Retry.", this);
            return;
        }
        restarting = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene.buildIndex);
    }
}
