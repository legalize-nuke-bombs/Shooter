using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Client.Aiming;
using Shooter.Client.Sounds;
using Shooter.Server.Worlds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Client.Worlds.Entities.Players
{
    public class PlayerRig
    {
        private const float LerpFactor = 15f;
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;

        public Aim Aim { get; }
        public bool UiCaptured { get; set; }

        private readonly ClientWorld world;
        private readonly Transform body;
        private readonly Transform cameraTransform;
        private readonly SpeakerView speaker;

        private float pitch;
        private bool jumpPending;
        private bool usePending;
        private bool reloadPending;
        private string speechPending;
        private bool positioned;
        private Vector3 targetPosition;

        public PlayerRig(Transform body, ClientWorld world)
        {
            this.body = body;
            this.world = world;
            cameraTransform = body.GetComponentInChildren<Camera>().transform;
            cameraTransform.localPosition = Vector3.up * Sight.EyeHeight;
            Aim = new Aim();
            speaker = new SpeakerView(body.gameObject);
        }

        public void Say(string speech)
        {
            speechPending = speech;
        }

        public void Tick(float deltaTime)
        {
            if (!UiCaptured) Look();
            Aim.Tick(body.position, pitch, body.eulerAngles.y);
            Reconcile();

            if (positioned)
                body.position = Vector3.Lerp(body.position, targetPosition, 1f - Mathf.Exp(-LerpFactor * deltaTime));
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
            if (keyboard.spaceKey.wasPressedThisFrame)
                jumpPending = true;
            if (keyboard.eKey.wasPressedThisFrame)
                usePending = true;
            if (keyboard.rKey.wasPressedThisFrame)
                reloadPending = true;
        }

        private void Reconcile()
        {
            EntityState me = world.Me;
            if (me == null) return;

            targetPosition = new Vector3(me.X, me.Y, me.Z);
            positioned = true;
            speaker.Apply(me.Part<SpeakerState>());
        }
    }
}
