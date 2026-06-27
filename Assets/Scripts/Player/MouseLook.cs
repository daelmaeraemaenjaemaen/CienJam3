using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private Camera playerCamera;

    [Header("Sensitivity")]
    [SerializeField] private float horizontalSensitivity = 150f;
    [SerializeField] private float verticalSensitivity = 250f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    private float verticalRotation;

    private void Awake()
    {
        if (playerBody == null)
            Debug.LogWarning("MouseLook: playerBody is not assigned.");

        if (playerCamera == null)
            Debug.LogWarning("MouseLook: playerCamera is not assigned.");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (playerBody == null || playerCamera == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;

        playerBody.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
