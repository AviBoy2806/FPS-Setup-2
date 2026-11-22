using UnityEngine;

// Transparent center with soft red edges; no Canvas or image asset required.
[DisallowMultipleComponent]
public class PlayerDamageVignette : MonoBehaviour
{
    [Min(0.01f)] public float Duration = 0.6f;
    [Range(0f, 1f)] public float Opacity = 0.75f;
    private Texture2D texture;
    private float remaining;

    public void Flash()
    {
        remaining = Mathf.Max(0.01f, Duration);
        if (texture != null) return;
        const int size = 128;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Player damage edges (runtime)";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = Mathf.Abs(x / (size - 1f) * 2f - 1f);
            float ny = Mathf.Abs(y / (size - 1f) * 2f - 1f);
            float edge = Mathf.Max(nx, ny);
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, edge));
            pixels[y * size + x] = new Color(1f, 0f, 0f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
    }

    private void Update() => remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);

    private void OnGUI()
    {
        if (remaining <= 0f || texture == null) return;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        // Draw behind the Game Over overlay.
        GUI.depth = 10;
        GUI.color = new Color(1f, 1f, 1f, Opacity * Mathf.Clamp01(remaining / Mathf.Max(0.01f, Duration)));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void OnDisable() => remaining = 0f;
    private void OnDestroy()
    {
        if (texture != null) Destroy(texture);
    }
}
