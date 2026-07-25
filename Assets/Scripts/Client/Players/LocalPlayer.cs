using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Game.Interacting;
using Shooter.Logging;

namespace Shooter.Client.Players
{
    [RequireComponent(typeof(Game.Movement.Movement))]
    [RequireComponent(typeof(Interactor))]
    public class LocalPlayer : NetworkBehaviour
    {
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;

        [SerializeField] private Camera view;

        private Game.Movement.Movement movement;
        private Interactor interactor;
        private float pitch;
        private float yaw;

        public bool Captured { get; set; }

        private void Awake()
        {
            movement = GetComponent<Game.Movement.Movement>();
            interactor = GetComponent<Interactor>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                view.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            NetworkManager.NetworkTickSystem.Tick += Send;
            Log.Info("Local player spawned as network object {} owned by client {}", NetworkObjectId, OwnerClientId);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            Cursor.lockState = CursorLockMode.None;
            NetworkManager.NetworkTickSystem.Tick -= Send;
            Log.Info("Local player despawned");
        }

        private void Update()
        {
            if (Captured) return;

            Look();
            Act();
        }

        private void LateUpdate()
        {
            view.transform.position = transform.position + Vector3.up * Interactor.EyeHeight;
            view.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void Look()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * LookSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -MaxPitch, MaxPitch);
        }

        private void Act()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard.spaceKey.wasPressedThisFrame) movement.JumpRpc();
            if (keyboard.eKey.wasPressedThisFrame) interactor.UseRpc();
        }

        private void Send()
        {
            movement.SteerRpc(Move(), yaw, pitch, Sprinting(), NetworkManager.LocalTime.Tick);
        }

        private Vector2 Move()
        {
            if (Captured) return Vector2.zero;

            Keyboard keyboard = Keyboard.current;
            return new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
        }

        private bool Sprinting()
        {
            return !Captured && Keyboard.current.leftShiftKey.isPressed;
        }
    }
}
