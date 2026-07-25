using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Game.Dying;
using Shooter.Game.Interacting;
using Shooter.Game.Sleeping;
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
        private Sleeper sleeper;
        private Game.Health.Health health;
        private Mortal mortal;
        private Game.Shooting.Shooter shooter;
        private float pitch;
        private float yaw;

        public bool Captured { get; set; }

        private void Awake()
        {
            movement = GetComponent<Game.Movement.Movement>();
            interactor = GetComponent<Interactor>();
            sleeper = GetComponent<Sleeper>();
            health = GetComponent<Game.Health.Health>();
            mortal = GetComponent<Mortal>();
            shooter = GetComponent<Game.Shooting.Shooter>();
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
            Mouse mouse = Mouse.current;

            if (keyboard.spaceKey.wasPressedThisFrame) movement.JumpRpc();
            if (mouse.leftButton.wasPressedThisFrame) shooter?.FireRpc();
            if (keyboard.rKey.wasPressedThisFrame) shooter?.ReloadRpc();
            if (!keyboard.eKey.wasPressedThisFrame) return;

            if (health != null && !health.Alive) mortal?.RiseRpc();
            else if (sleeper != null && sleeper.Sleeping) sleeper.WakeRpc();
            else interactor.UseRpc();
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
