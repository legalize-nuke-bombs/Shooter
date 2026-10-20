using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shooter.Game
{
    [RequireComponent(typeof(RpcMovement))]
    [RequireComponent(typeof(Interactor))]
    public class LocalPlayer : NetworkBehaviour
    {
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera view;
        [SerializeField] private EarSoundAsset saveSound;
        [SerializeField] private EarSoundAsset screenshotSound;

        private Controls controls;
        private EarSpeaker earSpeaker;
        private Gunner gunner;
        private Health health;
        private Interactor interactor;
        private Mortal mortal;
        private PlayerMouth playerMouth;
        private RpcMovement movement;
        private OwnRecoil ownRecoil;
        private Sleeper sleeper;
        private float pitch;
        private float yaw;
        private bool talking;
        private long? radioPartnerId;

        public bool IsInventoryOpen { get; private set; }

        public bool IsPauseMenuOpen { get; private set; }

        public bool IsInviteOpen { get; private set; }

        public bool IsRadioOpen { get; private set; }

        public bool IsPromptOpen { get; private set; }

        public long? RadioPartnerId => radioPartnerId;

        public long? TalkPartnerId
        {
            get
            {
                if (!talking) return null;
                if (radioPartnerId != null) return radioPartnerId;
                if (playerMouth == null || playerMouth.TalkerNetworkObjectId == 0) return null;
                if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerMouth.TalkerNetworkObjectId, out NetworkObject talkerNetworkObject)) return null;

                Character talkerCharacter = talkerNetworkObject.GetComponent<Character>();
                return talkerCharacter == null ? null : talkerCharacter.Id;
            }
        }

        private void Awake()
        {
            movement = GetComponent<RpcMovement>();
            interactor = GetComponent<Interactor>();
            playerMouth = GetComponent<PlayerMouth>();
            sleeper = GetComponent<Sleeper>();
            health = GetComponent<Health>();
            mortal = GetComponent<Mortal>();
            gunner = GetComponent<Gunner>();
            ownRecoil = GetComponent<OwnRecoil>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        private void Update()
        {
            if (controls == null) return;

            Vector2 lookDelta = controls.Player.Look.ReadValue<Vector2>() * LookSensitivity;

            yaw += lookDelta.x;
            pitch = Mathf.Clamp(pitch - lookDelta.y, -MaxPitch, MaxPitch);
        }

        private void LateUpdate()
        {
            Vector2 punchAngles = ownRecoil == null ? Vector2.zero : ownRecoil.PunchAngles;

            view.transform.position = transform.position + Vector3.up * Interactor.EyeHeight;
            view.transform.rotation = Quaternion.Euler(pitch - punchAngles.y, yaw + punchAngles.x, 0f);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                view.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            StartLocalControl();
        }

        public override void OnGainedOwnership()
        {
            if (!IsOwner) return;

            view.gameObject.SetActive(true);
            enabled = true;
            StartLocalControl();
        }

        public override void OnLostOwnership()
        {
            StopLocalControl();
            view.gameObject.SetActive(false);
            enabled = false;
        }

        public override void OnNetworkDespawn()
        {
            StopLocalControl();
        }

        private void StartLocalControl()
        {
            if (controls != null) return;

            yaw = transform.eulerAngles.y;
            movement.Turned += HandleTurned;

            controls = new Controls();
            controls.Player.Jump.performed += Jump;
            controls.Player.Attack.performed += PressTrigger;
            controls.Player.Attack.canceled += ReleaseTrigger;
            controls.Player.Reload.performed += ReloadWeapon;
            controls.Player.Interact.performed += RiseOrWakeOrInteract;
            controls.Player.Inventory.performed += OpenInventoryByKey;
            controls.UI.Inventory.performed += CloseInventoryByKey;
            controls.Player.Radio.performed += OpenRadioByKey;
            controls.UI.Radio.performed += CloseRadioByKey;
            controls.UI.Cancel.performed += CloseTopWindowOrTogglePauseMenu;
            controls.Game.Save.performed += QuickSave;
            controls.Game.Screenshot.performed += TakeScreenshot;
            controls.Game.Enable();

            if (playerMouth != null)
            {
                playerMouth.TalkOpened += HandleTalkOpened;
                playerMouth.TalkClosed += HandleTalkClosed;
            }

            GiveInputToPlayer();

            NetworkManager.NetworkTickSystem.Tick += SendSteering;
            Log.Info($"Local player active on network object {NetworkObjectId} owned by client {OwnerClientId}");
        }

        private void StopLocalControl()
        {
            if (controls == null) return;

            NetworkManager.NetworkTickSystem.Tick -= SendSteering;
            movement.Turned -= HandleTurned;

            controls.Player.Jump.performed -= Jump;
            controls.Player.Attack.performed -= PressTrigger;
            controls.Player.Attack.canceled -= ReleaseTrigger;
            controls.Player.Reload.performed -= ReloadWeapon;
            controls.Player.Interact.performed -= RiseOrWakeOrInteract;
            controls.Player.Inventory.performed -= OpenInventoryByKey;
            controls.UI.Inventory.performed -= CloseInventoryByKey;
            controls.Player.Radio.performed -= OpenRadioByKey;
            controls.UI.Radio.performed -= CloseRadioByKey;
            controls.UI.Cancel.performed -= CloseTopWindowOrTogglePauseMenu;
            controls.Game.Save.performed -= QuickSave;
            controls.Game.Screenshot.performed -= TakeScreenshot;

            if (playerMouth != null)
            {
                playerMouth.TalkOpened -= HandleTalkOpened;
                playerMouth.TalkClosed -= HandleTalkClosed;
            }

            // Disable before Dispose: Dispose only destroys the asset, and the generated finalizer asserts every map was disabled
            controls.Disable();
            controls.Dispose();
            controls = null;
            IsInventoryOpen = false;
            IsRadioOpen = false;
            IsPromptOpen = false;
            talking = false;
            radioPartnerId = null;

            SetCursorShown(true);
            Log.Info("Local player inactive");
        }

        private void HandleTurned(float turnedYaw)
        {
            yaw = turnedYaw;
        }

        private void RouteInputByOpenWindows()
        {
            if (controls == null) return;

            if (talking || IsPauseMenuOpen || IsInviteOpen)
            {
                GiveInputToWindow();
            }
            else if (IsInventoryOpen || IsRadioOpen || IsPromptOpen)
            {
                ShareInputWithWindow();
            }
            else
            {
                GiveInputToPlayer();
            }

            Log.Info($"Local player input is now {(talking ? "on the talk" : IsInviteOpen ? "on the invite window" : IsPauseMenuOpen ? "on the pause menu" : IsRadioOpen ? "on the radio" : IsPromptOpen ? "on a prompt" : IsInventoryOpen ? "shared with the inventory" : "back on the player")}");
        }

        private void GiveInputToPlayer()
        {
            controls.UI.Disable();
            controls.UI.Cancel.Enable();
            controls.Player.Enable();
            SetCursorShown(false);
        }

        private void ShareInputWithWindow()
        {
            controls.Player.Enable();
            controls.Player.Look.Disable();
            controls.Player.Attack.Disable();
            controls.Player.Reload.Disable();
            controls.Player.Interact.Disable();
            controls.Player.Inventory.Disable();
            if (IsRadioOpen) controls.Player.Radio.Disable();
            controls.UI.Enable();
            SetCursorShown(true);
        }

        private void GiveInputToWindow()
        {
            controls.Player.Disable();
            controls.UI.Enable();
            SetCursorShown(true);
        }

        private static void SetCursorShown(bool shown)
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
            ownRecoil?.PressTrigger();
        }

        private void ReleaseTrigger(InputAction.CallbackContext context)
        {
            gunner?.ReleaseTriggerRpc();
            ownRecoil?.ReleaseTrigger();
        }

        private void ReloadWeapon(InputAction.CallbackContext context)
        {
            gunner?.ReloadRpc();
        }

        private void OpenInventoryByKey(InputAction.CallbackContext context)
        {
            IsInventoryOpen = true;
            RouteInputByOpenWindows();
        }

        public void OpenInventoryFromTalk()
        {
            if (radioPartnerId != null) return;

            if (talking) playerMouth.HangUpRpc();

            IsInventoryOpen = true;
            RouteInputByOpenWindows();
            Log.Info("Inventory opened from the talk");
        }

        public void CloseInventory()
        {
            if (!IsInventoryOpen) return;

            IsInventoryOpen = false;
            IsPromptOpen = false;
            RouteInputByOpenWindows();
        }

        private void CloseInventoryByKey(InputAction.CallbackContext context)
        {
            if (IsPromptOpen) return;

            CloseInventory();
        }

        public void OpenPrompt()
        {
            if (IsPromptOpen) return;

            IsPromptOpen = true;
            RouteInputByOpenWindows();
        }

        public void ClosePrompt()
        {
            if (!IsPromptOpen) return;

            IsPromptOpen = false;
            RouteInputByOpenWindows();
        }

        private void HandleTalkOpened(ulong talkerId)
        {
            talking = true;
            RouteInputByOpenWindows();
        }

        private void HandleTalkClosed()
        {
            talking = false;
            RouteInputByOpenWindows();
        }

        private void OpenRadioByKey(InputAction.CallbackContext context)
        {
            OpenRadio();
        }

        private void CloseRadioByKey(InputAction.CallbackContext context)
        {
            CloseRadio();
        }

        public void OpenRadio()
        {
            IsRadioOpen = true;
            RouteInputByOpenWindows();
            Log.Info("Radio opened");
        }

        public void CloseRadio()
        {
            if (!IsRadioOpen) return;

            IsRadioOpen = false;
            RouteInputByOpenWindows();
            Log.Info("Radio closed");
        }

        public void OpenRadioTalk(long partnerId)
        {
            IsRadioOpen = false;
            radioPartnerId = partnerId;
            talking = true;
            RouteInputByOpenWindows();
            Log.Info($"Radio talk with {partnerId} opened");
        }

        public void CloseRadioTalk()
        {
            if (radioPartnerId == null) return;

            radioPartnerId = null;
            talking = false;
            RouteInputByOpenWindows();
            Log.Info("Radio talk closed");
        }

        private void CloseTopWindowOrTogglePauseMenu(InputAction.CallbackContext context)
        {
            if (IsPromptOpen)
            {
                ClosePrompt();
                return;
            }

            if (talking)
            {
                if (radioPartnerId != null)
                {
                    CloseRadioTalk();
                }
                else
                {
                    playerMouth.HangUpRpc();
                }

                return;
            }

            if (IsRadioOpen)
            {
                CloseRadio();
                return;
            }

            if (IsInventoryOpen)
            {
                CloseInventoryByKey(context);
                return;
            }

            if (IsInviteOpen)
            {
                CloseInvite();
                return;
            }

            if (IsPauseMenuOpen)
            {
                ClosePauseMenu();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        public void OpenInvite()
        {
            IsInviteOpen = true;
            RouteInputByOpenWindows();
            Log.Info("Invite window opened");
        }

        public void CloseInvite()
        {
            if (!IsInviteOpen) return;

            IsInviteOpen = false;
            RouteInputByOpenWindows();
            Log.Info("Invite window closed");
        }

        private void OpenPauseMenu()
        {
            IsPauseMenuOpen = true;
            RouteInputByOpenWindows();
            Log.Info("Pause menu opened");
        }

        public void ClosePauseMenu()
        {
            if (!IsPauseMenuOpen) return;

            IsPauseMenuOpen = false;
            RouteInputByOpenWindows();
            Log.Info("Pause menu closed");
        }

        public void SaveWorld()
        {
            if (!IsServer) return;

            Log.Info("Saving the world from the pause menu");
            StartCoroutine(SaveManager.SaveCoroutine());
        }

        private void QuickSave(InputAction.CallbackContext context)
        {
            if (!IsServer || SaveManager.IsSaving) return;

            if (earSpeaker != null) earSpeaker.PlayLocally(saveSound);
            Log.Info("Saving the world by the hotkey");
            StartCoroutine(SaveManager.SaveCoroutine());
        }

        private void TakeScreenshot(InputAction.CallbackContext context)
        {
            if (earSpeaker != null) earSpeaker.PlayLocally(screenshotSound);
            StartCoroutine(Screenshotter.TakeToScreenshotsFolder());
        }

        public void LeaveWorld()
        {
            if (IsServer)
            {
                Log.Info("Leaving the world from the pause menu, saving first");
                StartCoroutine(SaveWorldThenLeave());
                return;
            }

            Log.Info("Leaving the world from the pause menu");
            NetworkManager.Shutdown();
        }

        private IEnumerator SaveWorldThenLeave()
        {
            yield return SaveManager.SaveCoroutine();
            while (SaveManager.IsSaving)
            {
                yield return null;
            }

            NetworkManager.Shutdown();
        }

        private void RiseOrWakeOrInteract(InputAction.CallbackContext context)
        {
            if (health != null && !health.IsAlive)
            {
                mortal?.RiseRpc();
            }
            else if (sleeper != null && sleeper.IsSleeping)
            {
                sleeper.WakeRpc();
            }
            else
            {
                interactor.UseRpc();
            }
        }

        private void SendSteering()
        {
            Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
            bool sprinting = controls.Player.Sprint.IsPressed();

            movement.SteerRpc(moveInput, yaw, pitch, sprinting, NetworkManager.LocalTime.Tick);
        }
    }
}
