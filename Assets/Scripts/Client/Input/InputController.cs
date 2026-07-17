using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Server.Entities.Characters.Player;
using Shooter.Server.Worlds;

namespace Shooter.Client.Input
{
    public class InputController : MonoBehaviour
    {
        private const float LerpFactor = 15f;
        [SerializeField] private float lookSensitivity = 0.1f;

        private Transform cameraTransform;
        private float pitch;

        private NetworkClient networkClient;
        private bool netHooked;
        private bool jumpPending;
        private float nextInputSendTime;

        private bool positioned;
        private Vector3 targetPosition;

        private void Awake()
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            Look();
            SyncWithNet();
            if (positioned)
                transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-LerpFactor * Time.deltaTime));
        }

        private void Look()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -89f, 89f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                jumpPending = true;
        }

        private void SyncWithNet()
        {
            if (!netHooked)
            {
                if (NetworkClient.Instance == null) return;
                networkClient = NetworkClient.Instance;
                networkClient.SnapshotReceived += OnSnapshot;
                netHooked = true;
            }

            if (!networkClient.InWorld || Time.time < nextInputSendTime) return;

            nextInputSendTime = Time.time + 1f / NetworkClient.InputSendRate;
            Keyboard keyboard = Keyboard.current;
            networkClient.SendInput(new PlayerIntent
            {
                moveX = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                moveZ = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f),
                jump = jumpPending,
                sprint = keyboard.leftShiftKey.isPressed,
                yaw = transform.eulerAngles.y,
                pitch = pitch
            });
            jumpPending = false;
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            foreach (PlayerState playerState in snapshot.players)
            {
                if (playerState.id != networkClient.PlayerId) continue;
                targetPosition = new Vector3(playerState.x, playerState.y, playerState.z);
                positioned = true;
                break;
            }
        }

        private void OnDestroy()
        {
            if (netHooked)
                networkClient.SnapshotReceived -= OnSnapshot;
        }
    }
}
