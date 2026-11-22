using UnityEngine;

// Uses per-renderer overrides, leaving shared material assets untouched.
[DisallowMultipleComponent]
public class EnemyHitFlash : MonoBehaviour
{
    public Color FlashColor = Color.red;
    [Min(0.01f)] public float Duration = 0.15f;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private MaterialPropertyBlock[][] originals;
    private float remaining;
    private bool flashing;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originals = new MaterialPropertyBlock[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            originals[i] = new MaterialPropertyBlock[renderers[i].sharedMaterials.Length];
    }

    public void Flash()
    {
        remaining = Mathf.Max(0.01f, Duration);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material[] materials = renderers[i].sharedMaterials;
            for (int slot = 0; slot < originals[i].Length && slot < materials.Length; slot++)
            {
                Material material = materials[slot];
                if (material == null) continue;
                if (!flashing)
                {
                    originals[i][slot] = new MaterialPropertyBlock();
                    renderers[i].GetPropertyBlock(originals[i][slot], slot);
                }
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block, slot);
                if (material.HasProperty(BaseColor)) block.SetColor(BaseColor, FlashColor);
                if (material.HasProperty(ColorProperty)) block.SetColor(ColorProperty, FlashColor);
                renderers[i].SetPropertyBlock(block, slot);
            }
        }
        flashing = true;
    }

    private void Update()
    {
        if (!flashing) return;
        remaining -= Time.unscaledDeltaTime;
        if (remaining <= 0f) Restore();
    }

    private void Restore()
    {
        if (!flashing) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            for (int slot = 0; slot < originals[i].Length; slot++)
                renderers[i].SetPropertyBlock(originals[i][slot], slot);
        }
        flashing = false;
    }

    private void OnDisable() => Restore();
}
