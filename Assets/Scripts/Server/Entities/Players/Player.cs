using UnityEngine;
using Shooter.Logging;

namespace Shooter.Server.Entities.Players
{
    public class Player
    {
        private const float WalkSpeed = 5f;
        private const float SprintSpeed = 8f;
        private const float JumpHeight = 1.2f;
        private const float Gravity = -20f;

        public int ConnId { get; }
        public long UserId { get; }
        public string WorldId { get; }
        public string DisplayName { get; set; }
        public bool InWorld { get; set; }
        public GameObject Body { get; private set; }
        public PlayerIntent LastInput { get; private set; } = new PlayerIntent();

        private CharacterController controller;
        private float verticalVelocity;
        private bool jumpQueued;

        public Player(int connId, long userId, string worldId)
        {
            ConnId = connId;
            UserId = userId;
            WorldId = worldId;
            DisplayName = "player" + userId;
        }

        public void Tick(float dt)
        {
            Transform body = Body.transform;
            body.rotation = Quaternion.Euler(0f, LastInput.yaw, 0f);

            Vector3 direction = Vector3.ClampMagnitude(body.right * LastInput.moveX + body.forward * LastInput.moveZ, 1f);
            float speed = LastInput.sprint ? SprintSpeed : WalkSpeed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (jumpQueued)
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            jumpQueued = false;

            verticalVelocity += Gravity * dt;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * dt);
        }

        public void ApplyInput(PlayerIntent input)
        {
            input.moveX = Finite(input.moveX);
            input.moveZ = Finite(input.moveZ);
            input.yaw = Finite(input.yaw);
            input.pitch = Finite(input.pitch);
            LastInput = input;
            if (input.jump) jumpQueued = true;
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        public void Spawn()
        {
            Body = new GameObject("Sim_" + UserId);
            float angle = (ConnId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            controller = Body.AddComponent<CharacterController>();
            Log.Info("spawned body for user " + UserId + " world " + WorldId + " at " + Body.transform.position);
        }
    }
}
