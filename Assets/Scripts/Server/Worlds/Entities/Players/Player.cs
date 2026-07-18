using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Sleeping;

namespace Shooter.Server.Worlds.Entities.Players
{
    public class Player
    {
        private const float WalkSpeed = 5f;
        private const float SprintSpeed = 8f;
        private const float JumpHeight = 1.2f;
        private const float Gravity = -20f;
        private const float EyeHeight = 0.75f;

        public long UserId { get; }
        public string DisplayName { get; }
        public bool Sleeping { get; private set; }
        public GameObject Body { get; private set; }
        public PlayerIntent LastInput { get; private set; } = new PlayerIntent();

        private readonly CharacterController controller;
        private readonly Clock clock;
        private readonly PhysicsScene physics;
        private readonly ServerWorldPlayers worldPlayers;
        private float verticalVelocity;
        private bool jumpQueued;

        public Player(long userId, string displayName, Scene scene, Clock clock, ServerWorldPlayers worldPlayers)
        {
            UserId = userId;
            DisplayName = displayName;
            this.clock = clock;
            this.worldPlayers = worldPlayers;
            physics = scene.GetPhysicsScene();

            Body = new GameObject("Player_" + userId);
            float angle = (userId * 137f) % 360f;
            Vector3 spread = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            controller = Body.AddComponent<CharacterController>();
            SceneManager.MoveGameObjectToScene(Body, scene);
            Log.Info("User " + userId + " body spawned at " + Body.transform.position);
        }

        public void Destroy()
        {
            if (Body != null) Object.Destroy(Body);
            Body = null;
        }

        public void WakeUp()
        {
            if (!Sleeping) return;
            Sleeping = false;
            Log.Info("User " + UserId + " woke up");
        }

        public void Tick(float dt)
        {
            if (Sleeping) return;

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

            if (Sleeping)
            {
                if ((input.Use || input.Jump) && !worldPlayers.AllAsleep()) WakeUp();
                return;
            }
            if (input.Use)
            {
                TrySleep();
                if (Sleeping) return;
            }
            if (input.Jump) jumpQueued = true;
        }

        private void TrySleep()
        {
            if (!clock.IsNight())
            {
                Log.Info("User " + UserId + " tried to sleep in daytime, ignored");
                return;
            }
            if (!LookingAtBed())
            {
                Log.Info("User " + UserId + " tried to sleep with no bed in sight, ignored");
                return;
            }
            Sleeping = true;
            Log.Info("User " + UserId + " fell asleep at " + Body.transform.position);
        }

        private bool LookingAtBed()
        {
            Ray look = LookRay();
            return physics.Raycast(look.origin, look.direction, out RaycastHit hit, Sleep.UseReach)
                   && Sleep.IsBed(hit.transform.name);
        }

        private Ray LookRay()
        {
            Vector3 eyes = Body.transform.position + Vector3.up * EyeHeight;
            Quaternion look = Quaternion.Euler(LastInput.Pitch, LastInput.Yaw, 0f);
            return new Ray(eyes, look * Vector3.forward);
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        public PlayerState State()
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
                Pitch = LastInput.Pitch,
                Sleeping = Sleeping
            };
        }
    }
}
