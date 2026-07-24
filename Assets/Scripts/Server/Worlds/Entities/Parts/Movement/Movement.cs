using UnityEngine;
using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Movement
{
    public sealed class Movement : Part
    {
        private const float Gravity = -20f;
        private const float JumpHeight = 1.2f;
        private const float GroundedFall = -2f;

        private readonly CharacterController controller;

        private Vector3 velocity;
        private bool jumpQueued;
        private float verticalVelocity;

        public Movement(Entity self) : base(self, typeof(Movement))
        {
            controller = self.Attach<CharacterController>();
        }

        public bool Grounded => controller.isGrounded;

        public float GroundTravel { get; private set; }

        public void Face(float yaw)
        {
            controller.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void Steer(float forward, float right, float speed)
        {
            Transform body = controller.transform;
            Vector3 direction = Vector3.ClampMagnitude(body.forward * forward + body.right * right, 1f);
            velocity = direction * speed;
        }

        public void Jump()
        {
            jumpQueued = true;
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            controller.transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
            velocity = Vector3.zero;
            Log.Info("Entity {} teleported to {}", Self.Name, position);
        }

        public override void Tick(float dt)
        {
            if (controller.isGrounded)
            {
                verticalVelocity = GroundedFall;
                if (jumpQueued) verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            jumpQueued = false;
            verticalVelocity += Gravity * dt;

            Vector3 before = controller.transform.position;
            controller.Move((velocity + Vector3.up * verticalVelocity) * dt);
            velocity = Vector3.zero;

            Vector3 moved = controller.transform.position - before;
            moved.y = 0f;
            GroundTravel = controller.isGrounded ? moved.magnitude : 0f;
        }
    }
}
