using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float gravity = -9.81f;

    private float verticalVelocity;

    private void Awake()
    {
        if (characterController == null)
            Debug.LogWarning("PlayerController: characterController is not assigned.");
    }

    private void Update()
    {
        if (characterController == null)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
