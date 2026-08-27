using System.Collections;
using Shooter.Game.Body;
using Shooter.Game.Combat;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
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
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera view;
        private Controls controls;
        private Gunner gunner;
        private Health health;
        private Interactor interactor;
        private Mortal mortal;
        private Mouth mouth;

        private Movement movement;
        private float pitch;
        private OwnRecoil recoil;
        private Sleeper sleeper;
        private bool talking;
        private float yaw;

        public bool InventoryOpen { get; private set; }

        public bool Paused { get; private set; }

        public bool Inviting { get; private set; }

        private void Awake()
        {
            movement = GetComponent<Movement>();
            interactor = GetComponent<Interactor>();
            mouth = GetComponent<Mouth>();
            sleeper = GetComponent<Sleeper>();
            health = GetComponent<Health>();
            mortal = GetComponent<Mortal>();
            gunner = GetComponent<Gunner>();
            recoil = GetComponent<OwnRecoil>();
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
            Vector2 punch = recoil == null ? Vector2.zero : recoil.Punch;

            view.transform.position = transform.position + Vector3.up * Interactor.EyeHeight;
            view.transform.rotation = Quaternion.Euler(pitch - punch.y, yaw + punch.x, 0f);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                view.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            Activate();
        }

        public override void OnGainedOwnership()
        {
            if (!IsOwner) return;

            view.gameObject.SetActive(true);
            enabled = true;
            Activate();
        }

        public override void OnLostOwnership()
        {
            Deactivate();
            view.gameObject.SetActive(false);
            enabled = false;
        }

        public override void OnNetworkDespawn()
        {
            Deactivate();
        }

        private void Activate()
        {
            if (controls != null) return;

            yaw = transform.eulerAngles.y;
            movement.Turned += Turn;

            controls = new Controls();
            controls.Player.Jump.performed += Jump;
            controls.Player.Attack.performed += PressTrigger;
            controls.Player.Attack.canceled += ReleaseTrigger;
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
            Log.Info($"Local player active on network object {NetworkObjectId} owned by client {OwnerClientId}");
        }

        private void Deactivate()
        {
            if (controls == null) return;

            NetworkManager.NetworkTickSystem.Tick -= Send;
            movement.Turned -= Turn;

            controls.Player.Jump.performed -= Jump;
            controls.Player.Attack.performed -= PressTrigger;
            controls.Player.Attack.canceled -= ReleaseTrigger;
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

            Point(true);
            Log.Info("Local player inactive");
        }

        private void Turn(float turned)
        {
            yaw = turned;
        }

        private void Capture()
        {
            if (controls == null) return;

            if (talking || Paused || Inviting) Listen();
            else if (InventoryOpen) Browse();
            else Grab();

            Log.Info(
                $"Local player input is now {(talking ? "on the talk" : Inviting ? "on the invite window" : Paused ? "on the pause menu" : InventoryOpen ? "shared with the bag" : "back on the player")}");
        }

        private void Grab()
        {
            controls.UI.Disable();
            controls.UI.Cancel.Enable();
            controls.Player.Enable();
            Point(false);
        }

        private void Browse()
        {
            controls.Player.Enable();
            controls.Player.Look.Disable();
            controls.Player.Attack.Disable();
            controls.Player.Reload.Disable();
            controls.Player.Interact.Disable();
            controls.Player.Inventory.Disable();
            controls.UI.Enable();
            Point(true);
        }

        private void Listen()
        {
            controls.Player.Disable();
            controls.UI.Enable();
            Point(true);
        }

        private static void Point(bool shown)
        {
            Cursor.lockState = shown ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shown;
        }

        private void Jump(InputAction.CallbackContext context)
        {
            movement.JumpRpc();
        }

        private void PressTrigger(InputAction.CallbackContext context)
        {
            gunner?.PressTriggerRpc();
            recoil?.Press();
        }

        private void ReleaseTrigger(InputAction.CallbackContext context)
        {
            gunner?.ReleaseTriggerRpc();
            recoil?.Release();
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

            if (Inviting)
            {
                CloseInvite();
                return;
            }

            if (Paused) Resume();
            else OpenPause();
        }

        public void OpenInvite()
        {
            Inviting = true;
            Capture();
            Log.Info("Invite window opened");
        }

        public void CloseInvite()
        {
            if (!Inviting) return;

            Inviting = false;
            Capture();
            Log.Info("Invite window closed");
        }

        private void OpenPause()
        {
            Paused = true;
            Capture();
            Log.Info("Pause menu opened");
        }

        public void Resume()
        {
            if (!Paused) return;

            Paused = false;
            Capture();
            Log.Info("Pause menu closed");
        }

        public void SaveWorld()
        {
            if (!IsServer) return;

            Log.Info("Saving the world from the pause menu");
            StartCoroutine(SaveManager.Current.SaveCoroutine());
        }

        public void LeaveWorld()
        {
            if (IsServer)
            {
                Log.Info("Leaving the world from the pause menu, saving first");
                StartCoroutine(SaveThenLeave());
                return;
            }

            Log.Info("Leaving the world from the pause menu");
            NetworkManager.Shutdown();
        }

        private IEnumerator SaveThenLeave()
        {
            yield return SaveManager.Current.SaveCoroutine();
            while (SaveManager.Current.Saving) yield return null;

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
