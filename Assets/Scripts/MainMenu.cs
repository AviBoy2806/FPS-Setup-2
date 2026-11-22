using UnityEngine;
using UnityEngine.SceneManagement;

// Uses the same simple GUI approach as the existing game-over screen.
[RequireComponent(typeof(Camera))]
public class MainMenu : MonoBehaviour
{
    private const string GameScene = "Assets/Scenes/GameScene.unity";
    private bool loading;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera menuCamera = GetComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.035f, 0.05f, 0.075f);
        menuCamera.cullingMask = 0;
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(0.6f, 0.68f, 0.76f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { background = Texture2D.whiteTexture, textColor = new Color(0.03f, 0.08f, 0.1f) },
                hover = { background = Texture2D.whiteTexture, textColor = new Color(0.03f, 0.08f, 0.1f) },
                active = { background = Texture2D.whiteTexture, textColor = Color.white }
            };
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        Color previousBackground = GUI.backgroundColor;
        bool previousEnabled = GUI.enabled;
        try
        {
            // Keep the menu centered and readable at different window sizes.
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Vector3 offset = new Vector3((Screen.width - 1280f * scale) * 0.5f,
                (Screen.height - 720f * scale) * 0.5f, 0f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));

            GUI.color = new Color(0.25f, 0.85f, 0.8f);
            GUI.DrawTexture(new Rect(610f, 218f, 60f, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(240f, 245f, 800f, 90f), "FPS SETUP", titleStyle);
            GUI.Label(new Rect(240f, 338f, 800f, 35f), "READY WHEN YOU ARE", subtitleStyle);

            Rect playButton = new Rect(490f, 414f, 300f, 64f);
            GUI.backgroundColor = playButton.Contains(Event.current.mousePosition)
                ? new Color(0.4f, 1f, 0.92f) : new Color(0.25f, 0.85f, 0.8f);
            GUI.enabled = !loading;
            if (GUI.Button(playButton, loading ? "Loading..." : "Play", buttonStyle)) Play();
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.enabled = previousEnabled;
        }
    }

    public void Play()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(GameScene))
        {
            Debug.LogError("Add GameScene to Build Profiles > Scene List to play.", this);
            return;
        }

        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
    }
}
