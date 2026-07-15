using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float lookSensitivity = 0.1f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float pitch;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -89f, 89f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Keyboard kb = Keyboard.current;
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 direction = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
