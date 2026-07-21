using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Server.Worlds.Entities.Parts.Pilot
{
    public sealed class Pilot : Part
    {
        private const float WalkSpeed = 5f;
        private const float SprintSpeed = 8f;
        private const float JumpHeight = 1.2f;
        private const float Gravity = -20f;
        private const float StrideLength = 2f;

        public bool Sleeping { get; private set; }
        public PlayerIntent LastInput { get; private set; } = new PlayerIntent();

        private readonly CharacterController controller;
        private readonly Clock clock;
        private readonly Sight sight;
        private readonly WorldEntities worldEntities;
        private readonly Speaker.Speaker speaker;

        private float verticalVelocity;
        private bool jumpQueued;
        private float strideProgress;

        public Pilot(CharacterController controller, Clock clock, Sight sight, WorldEntities worldEntities, Speaker.Speaker speaker)
        {
            this.controller = controller;
            this.clock = clock;
            this.sight = sight;
            this.worldEntities = worldEntities;
            this.speaker = speaker;
        }

        public void Apply(PlayerIntent input)
        {
            input.MoveX = Finite(input.MoveX);
            input.MoveZ = Finite(input.MoveZ);
            input.Yaw = Finite(input.Yaw);
            input.Pitch = Finite(input.Pitch);
            LastInput = input;

            if (Sleeping)
            {
                if ((input.Use || input.Jump) && !worldEntities.AllAsleep()) WakeUp();
                return;
            }
            if (input.Use)
            {
                TrySleep();
                if (Sleeping) return;
            }
            if (input.Jump) jumpQueued = true;
        }

        public void WakeUp()
        {
            if (!Sleeping) return;
            Sleeping = false;
            Log.Info("Pilot at {} woke up", controller.transform.position);
        }

        public override PartState State()
        {
            return new PilotState { Pitch = LastInput.Pitch, Sleeping = Sleeping };
        }

        public override void Tick(Entity self, float dt)
        {
            if (Sleeping) return;

            Transform body = controller.transform;
            body.rotation = Quaternion.Euler(0f, LastInput.Yaw, 0f);

            Vector3 direction = Vector3.ClampMagnitude(body.right * LastInput.MoveX + body.forward * LastInput.MoveZ, 1f);
            float speed = LastInput.Sprint ? SprintSpeed : WalkSpeed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (jumpQueued)
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            jumpQueued = false;

            verticalVelocity += Gravity * dt;
            Vector3 before = body.position;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * dt);
            Step(body.position - before);
        }

        private void Step(Vector3 moved)
        {
            if (!controller.isGrounded) return;
            moved.y = 0f;
            strideProgress += moved.magnitude;
            if (strideProgress < StrideLength) return;
            strideProgress -= StrideLength;
            speaker.Play(SoundType.Footsteps);
        }

        private void TrySleep()
        {
            if (!clock.IsNight())
            {
                Log.Info("Pilot tried to sleep in daytime, ignored");
                return;
            }
            if (!LookingAtBed())
            {
                Log.Info("Pilot tried to sleep with no bed in sight, ignored");
                return;
            }
            Sleeping = true;
            Log.Info("Pilot fell asleep at {}", controller.transform.position);
        }

        private bool LookingAtBed()
        {
            Ray look = Sight.LookRay(controller.transform.position, LastInput.Pitch, LastInput.Yaw);
            return sight.Cast(look, Sleep.UseReach, out RaycastHit hit) && Sleep.IsBed(hit);
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }
    }
}
