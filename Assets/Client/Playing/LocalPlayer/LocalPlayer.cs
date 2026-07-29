using Shooter.Game.Body;
using Shooter.Game.Body.Sleeping;
using Shooter.Game.Combat;
using Shooter.Game.Speech;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shooter.Client.Playing
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
        private Mouth mouth;
        private Sleeper sleeper;
        private Health health;
        private Mortal mortal;
        private Gunner gunner;
        private Controls controls;
        private bool captured;
        private bool talking;
        private float pitch;
        private float yaw;

        public bool InventoryOpen { get; private set; }

        private void Awake()
        {
            movement = GetComponent<Movement>();
            interactor = GetComponent<Interactor>();
            mouth = GetComponent<Mouth>();
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
            controls.UI.Cancel.performed += Escape;

            if (mouth != null)
            {
                mouth.Opened += OpenTalk;
                mouth.Closed += CloseTalk;
            }

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
            controls.UI.Cancel.performed -= Escape;

            if (mouth != null)
            {
                mouth.Opened -= OpenTalk;
                mouth.Closed -= CloseTalk;
            }

            controls.Dispose();
            controls = null;
            InventoryOpen = false;
            talking = false;

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

        private void Capture()
        {
            bool wanted = InventoryOpen || talking;
            if (captured == wanted || controls == null) return;

            captured = wanted;

            if (wanted) Release();
            else Grab();

            Log.Info("Local player input is now {}", wanted ? "released to the interface" : "back on the player");
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
            Capture();
        }

        public void CloseInventory()
        {
            if (!InventoryOpen) return;

            InventoryOpen = false;
            Capture();
        }

        private void CloseBag(InputAction.CallbackContext context)
        {
            CloseInventory();
        }

        private void OpenTalk(ulong talkerId)
        {
            talking = true;
            Capture();
        }

        private void CloseTalk()
        {
            talking = false;
            Capture();
        }

        private void Escape(InputAction.CallbackContext context)
        {
            if (talking)
            {
                mouth.HangUpRpc();
                return;
            }

            if (InventoryOpen)
            {
                CloseBag(context);
                return;
            }

            Log.Info("Escape with nothing open, leaving the world for the menu");
            NetworkManager.Shutdown();
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
