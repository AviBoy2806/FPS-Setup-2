using UnityEngine;

// Procedural view-model motion, driven by actual shots and the reload state.
[DisallowMultipleComponent]
public class GunAnimation : MonoBehaviour
{
    [Tooltip("Defaults to this gun's transform. Use a dedicated visual pivot if needed.")]
    public Transform Visual;
    [Header("Shoot")]
    public Vector3 RecoilOffset = new Vector3(0f, 0.012f, -0.065f);
    public Vector3 RecoilAngles = new Vector3(-7f, 0f, 1f);
    [Min(0.1f)] public float RecoilReturnSpeed = 18f;
    [Header("Reload")]
    public Vector3 ReloadOffset = new Vector3(0f, -0.22f, -0.08f);
    public Vector3 ReloadAngles = new Vector3(20f, 0f, -25f);

    private Gun gun;
    private Transform capturedVisual;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float recoil;
    private float reloadStart;
    private float reloadDuration;
    private bool reloading;

    private void Awake()
    {
        gun = GetComponent<Gun>();
        CapturePose();
    }

    private void CapturePose()
    {
        capturedVisual = Visual != null ? Visual : transform;
        restPosition = capturedVisual.localPosition;
        restRotation = capturedVisual.localRotation;
    }

    public void PlayShot()
    {
        if (!enabled) return;
        recoil = Mathf.Min(recoil + 1f, 2f);
    }

    public void BeginReload(float duration)
    {
        recoil = 0f;
        reloadStart = Time.time;
        reloadDuration = Mathf.Max(0.01f, duration);
        reloading = true;
    }

    public void EndReload()
    {
        reloading = false;
        ResetPose();
    }

    private void LateUpdate()
    {
        Transform desiredVisual = Visual != null ? Visual : transform;
        if (desiredVisual != capturedVisual)
        {
            ResetPose();
            CapturePose();
        }
        if (gun == null || !gun.enabled)
        {
            Cancel();
            return;
        }

        float reloadWeight = 0f;
        if (reloading)
        {
            float t = Mathf.Clamp01((Time.time - reloadStart) / reloadDuration);
            // Lower and tilt, hold, then bring the gun back before reload completes.
            if (t < 0.25f) reloadWeight = Mathf.SmoothStep(0f, 1f, t / 0.25f);
            else if (t < 0.7f) reloadWeight = 1f;
            else reloadWeight = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f);
        }
        Vector3 offset = RecoilOffset * recoil + ReloadOffset * reloadWeight;
        Vector3 angles = RecoilAngles * recoil + ReloadAngles * reloadWeight;

        // Interpret offsets in camera axes, even when the imported gun faces sideways.
        Quaternion axes = restRotation;
        if (gun.playerCamera != null)
        {
            Quaternion parentRotation = capturedVisual.parent != null
                ? capturedVisual.parent.rotation : Quaternion.identity;
            axes = Quaternion.Inverse(parentRotation) * gun.playerCamera.transform.rotation;
        }
        capturedVisual.localPosition = restPosition + axes * offset;
        capturedVisual.localRotation = axes * Quaternion.Euler(angles) * Quaternion.Inverse(axes) * restRotation;
        recoil = Mathf.Lerp(recoil, 0f, 1f - Mathf.Exp(-RecoilReturnSpeed * Time.deltaTime));
    }

    private void ResetPose()
    {
        if (capturedVisual == null) return;
        capturedVisual.localPosition = restPosition;
        capturedVisual.localRotation = restRotation;
    }

    public void Cancel()
    {
        recoil = 0f;
        reloading = false;
        ResetPose();
    }

    private void OnDisable() => Cancel();
}
