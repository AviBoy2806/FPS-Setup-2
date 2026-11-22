using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public float Sensitivity = 0.1f;

    public Transform PlayerTransform;
    public Transform CameraTransform;

    private Vector2 mouseInput;

    private float pitch;
    private float yaw;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerTransform == null)
            PlayerTransform = transform;

        yaw = PlayerTransform.eulerAngles.y;

        pitch = CameraTransform.localEulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;
    }

    void Update()
    {
        HandleLook();
    }

    public void OnLook(InputValue value)
    {
        mouseInput = value.Get<Vector2>();
    }

    void HandleLook()
    {
        yaw += mouseInput.x * Sensitivity;
        pitch -= mouseInput.y * Sensitivity;

        pitch = Mathf.Clamp(pitch, -89f, 89f);

        PlayerTransform.rotation = Quaternion.Euler(
            0f,
            yaw,
            0f
        );

        CameraTransform.localRotation = Quaternion.Euler(
            pitch,
            0f,
            0f
        );
    }
}