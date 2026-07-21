using UnityEngine;
using UnityEngine.InputSystem;
using Shooter.Client.Aiming;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;

namespace Shooter.Client.Worlds.Entities.Players
{
    public class PlayerRig
    {
        private const float LerpFactor = 15f;
        private const float LookSensitivity = 0.1f;
        private const float MaxPitch = 89f;

        public Aim Aim { get; }

        private readonly ClientWorld world;
        private readonly Transform body;
        private readonly Transform cameraTransform;

        private float pitch;
        private bool jumpPending;
        private bool usePending;
        private bool positioned;
        private Vector3 targetPosition;

        public PlayerRig(Transform body, ClientWorld world)
        {
            this.body = body;
            this.world = world;
            cameraTransform = body.GetComponentInChildren<Camera>().transform;
            Aim = new Aim(cameraTransform);
        }

        public void Tick(float deltaTime)
        {
            Look();
            Aim.Tick();
            Reconcile();

            if (positioned)
                body.position = Vector3.Lerp(body.position, targetPosition, 1f - Mathf.Exp(-LerpFactor * deltaTime));
        }

        public PlayerIntent BuildIntent()
        {
            Keyboard keyboard = Keyboard.current;
            var intent = new PlayerIntent
            {
                MoveX = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                MoveZ = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f),
                Jump = jumpPending,
                Sprint = keyboard.leftShiftKey.isPressed,
                Use = usePending,
                Yaw = body.eulerAngles.y,
                Pitch = pitch
            };
            jumpPending = false;
            usePending = false;
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
        }

        private void Reconcile()
        {
            EntityState me = world.Me;
            if (me == null) return;

            targetPosition = new Vector3(me.X, me.Y, me.Z);
            positioned = true;
        }
    }
}
