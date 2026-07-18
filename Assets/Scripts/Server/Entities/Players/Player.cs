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

        public long UserId { get; }
        public string DisplayName { get; set; }
        public GameObject Body { get; private set; }
        public PlayerIntent LastInput { get; private set; } = new PlayerIntent();

        private CharacterController controller;
        private float verticalVelocity;
        private bool jumpQueued;

        public Player(long userId)
        {
            UserId = userId;
            DisplayName = "player" + userId;
        }

        public void Tick(float dt)
        {
            Transform body = Body.transform;
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
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * dt);
        }

        public void ApplyInput(PlayerIntent input)
        {
            input.MoveX = Finite(input.MoveX);
            input.MoveZ = Finite(input.MoveZ);
            input.Yaw = Finite(input.Yaw);
            input.Pitch = Finite(input.Pitch);
            LastInput = input;
            if (input.Jump) jumpQueued = true;
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        public PlayerState ToState()
        {
            Vector3 position = Body.transform.position;
            return new PlayerState
            {
                Id = UserId,
                Name = DisplayName,
                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = Body.transform.eulerAngles.y,
                Pitch = LastInput.Pitch
            };
        }

        public void Spawn()
        {
            Body = new GameObject("Sim_" + UserId);
            float angle = (UserId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            controller = Body.AddComponent<CharacterController>();
            Log.Info("spawned body for user " + UserId + " at " + Body.transform.position);
        }
    }
}
