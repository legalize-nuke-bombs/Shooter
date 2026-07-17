using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Net;

namespace Shooter.Entities.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float lookSensitivity = 0.1f;

        private CharacterController controller;
        private Transform cameraTransform;
        private float pitch;
        private float verticalVelocity;

        private NetworkClient net;
        private bool jumpPendingForNet;
        private float nextInputSendTime;

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
            SyncWithNet();
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
            var input = new MotorInput
            {
                MoveX = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                MoveZ = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f),
                Sprint = kb.leftShiftKey.isPressed,
                Jump = kb.spaceKey.wasPressedThisFrame,
                Yaw = transform.eulerAngles.y
            };
            if (kb.spaceKey.wasPressedThisFrame)
                jumpPendingForNet = true;

            PlayerMotor.Step(controller, ref verticalVelocity, input, Time.deltaTime);
        }

        private void SyncWithNet()
        {
            if (net == null)
            {
                if (NetworkClient.Instance == null) return;
                net = NetworkClient.Instance;
                net.WorldJoined += OnWorldJoined;
            }

            if (!net.InWorld || Time.time < nextInputSendTime) return;

            nextInputSendTime = Time.time + 1f / NetworkClient.InputSendRate;
            Keyboard kb = Keyboard.current;
            net.SendInput(new InputMsg
            {
                moveX = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                moveZ = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f),
                jump = jumpPendingForNet,
                sprint = kb.leftShiftKey.isPressed,
                yaw = transform.eulerAngles.y,
                pitch = pitch
            });
            jumpPendingForNet = false;
        }

        private void OnWorldJoined(WorldJoinedMsg joined)
        {
            foreach (PlayerStateMsg p in joined.players)
            {
                if (p.id != net.PlayerId) continue;
                controller.enabled = false;
                transform.position = new Vector3(p.x, p.y, p.z);
                controller.enabled = true;
                break;
            }
        }

        private void OnDestroy()
        {
            if (net != null)
                net.WorldJoined -= OnWorldJoined;
        }
    }
}
