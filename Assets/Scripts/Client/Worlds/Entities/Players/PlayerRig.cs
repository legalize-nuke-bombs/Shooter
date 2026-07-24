using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Client.Aiming;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds;

namespace Shooter.Client.Worlds.Entities.Players
{
    public class PlayerRig
    {
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;

        private readonly Transform body;
        private readonly Transform cameraTransform;

        private float pitch;
        private bool jumpPending;
        private bool usePending;
        private bool reloadPending;
        private string speechPending;

        public PlayerRig(Transform body)
        {
            this.body = body;
            cameraTransform = body.GetComponentInChildren<Camera>().transform;
            cameraTransform.localPosition = Vector3.up * Sight.EyeHeight;
            Aim = new Aim();
        }

        public Aim Aim { get; }

        public Transform Body => body;

        public bool UiCaptured { get; set; }

        public void Say(string speech)
        {
            speechPending = speech;
        }

        public void Tick(float dt)
        {
            if (!UiCaptured) Look();

            Aim.At(body.position, pitch, body.eulerAngles.y);
        }

        public PlayerIntent BuildIntent()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            var intent = new PlayerIntent
            {
                MoveX = UiCaptured ? 0f : (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                MoveZ = UiCaptured ? 0f : (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f),
                Sprint = !UiCaptured && keyboard.leftShiftKey.isPressed,
                Yaw = body.eulerAngles.y,
                Pitch = pitch,
                Jump = jumpPending,
                Use = usePending,
                Shoot = !UiCaptured && mouse.leftButton.isPressed,
                Reload = reloadPending,
                Speech = speechPending
            };

            jumpPending = false;
            usePending = false;
            reloadPending = false;
            speechPending = null;
            return intent;
        }

        private void Look()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * LookSensitivity;
            body.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -MaxPitch, MaxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Keyboard keyboard = Keyboard.current;
            if (keyboard.spaceKey.wasPressedThisFrame) jumpPending = true;
            if (keyboard.eKey.wasPressedThisFrame) usePending = true;
            if (keyboard.rKey.wasPressedThisFrame) reloadPending = true;
        }
    }
}
