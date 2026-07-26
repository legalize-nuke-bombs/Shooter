using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Client.Controlling;
using Shooter.Game.Dying;
using Shooter.Game.Interacting;
using Shooter.Game.Moving;
using Shooter.Game.Shooting;
using Shooter.Game.Sleeping;
using Shooter.Game.Vitals;
using Shooter.Logging;

namespace Shooter.Client.Players
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Interactor))]
    public class LocalPlayer : NetworkBehaviour
    {
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;

        [SerializeField] private Camera view;

        private Movement movement;
        private Interactor interactor;
        private Sleeper sleeper;
        private Health health;
        private Mortal mortal;
        private Gunner gunner;
        private Controls controls;
        private bool captured;
        private float pitch;
        private float yaw;

        public bool InventoryOpen { get; private set; }

        public bool Captured
        {
            get => captured;
            set
            {
                if (captured == value || controls == null) return;

                captured = value;

                if (value) Release();
                else Grab();

                Log.Info("Local player input is now {}", value ? "released to the interface" : "back on the player");
            }
        }

        private void Awake()
        {
            movement = GetComponent<Movement>();
            interactor = GetComponent<Interactor>();
            sleeper = GetComponent<Sleeper>();
            health = GetComponent<Health>();
            mortal = GetComponent<Mortal>();
            gunner = GetComponent<Gunner>();
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

            controls = new Controls();
            controls.Player.Jump.performed += Jump;
            controls.Player.Attack.performed += Fire;
            controls.Player.Reload.performed += Reload;
            controls.Player.Interact.performed += Use;
            controls.Player.Inventory.performed += OpenBag;
            controls.UI.Inventory.performed += CloseBag;
            Grab();

            NetworkManager.NetworkTickSystem.Tick += Send;
            Log.Info("Local player spawned as network object {} owned by client {}", NetworkObjectId, OwnerClientId);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            NetworkManager.NetworkTickSystem.Tick -= Send;

            controls.Player.Jump.performed -= Jump;
            controls.Player.Attack.performed -= Fire;
            controls.Player.Reload.performed -= Reload;
            controls.Player.Interact.performed -= Use;
            controls.Player.Inventory.performed -= OpenBag;
            controls.UI.Inventory.performed -= CloseBag;
            controls.Dispose();
            controls = null;
            InventoryOpen = false;

            Cursor.lockState = CursorLockMode.None;
            Log.Info("Local player despawned");
        }

        private void Update()
        {
            if (controls == null) return;

            Vector2 delta = controls.Player.Look.ReadValue<Vector2>() * LookSensitivity;

            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -MaxPitch, MaxPitch);
        }

        private void LateUpdate()
        {
            view.transform.position = transform.position + Vector3.up * Interactor.EyeHeight;
            view.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void Grab()
        {
            controls.UI.Disable();
            controls.Player.Enable();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Release()
        {
            controls.Player.Disable();
            controls.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
        }

        private void Jump(InputAction.CallbackContext context)
        {
            movement.JumpRpc();
        }

        private void Fire(InputAction.CallbackContext context)
        {
            gunner?.FireRpc();
        }

        private void Reload(InputAction.CallbackContext context)
        {
            gunner?.ReloadRpc();
        }

        private void OpenBag(InputAction.CallbackContext context)
        {
            InventoryOpen = true;
            Captured = true;
        }

        private void CloseBag(InputAction.CallbackContext context)
        {
            InventoryOpen = false;
            Captured = false;
        }

        private void Use(InputAction.CallbackContext context)
        {
            if (health != null && !health.Alive) mortal?.RiseRpc();
            else if (sleeper != null && sleeper.Sleeping) sleeper.WakeRpc();
            else interactor.UseRpc();
        }

        private void Send()
        {
            Vector2 move = controls.Player.Move.ReadValue<Vector2>();
            bool sprinting = controls.Player.Sprint.IsPressed();

            movement.SteerRpc(move, yaw, pitch, sprinting, NetworkManager.LocalTime.Tick);
        }
    }
}
