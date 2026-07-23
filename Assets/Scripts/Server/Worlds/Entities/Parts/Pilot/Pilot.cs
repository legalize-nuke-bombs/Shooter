using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Entities.Parts.Speaker;
using Shooter.Server.Worlds.Entities.Spawning;
using UnityEngine.SceneManagement;

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
        private readonly Health.Health health;
        private readonly Inventory.Inventory inventory;
        private readonly Speaker.Speaker speaker;
        private readonly Shooter.Shooter shooter;
        private readonly Hands.Hands hands;
        private Vector3 spawnPoint = new Vector3(0, 0, 0);

        private readonly Clock clock;
        private readonly Sight sight;
        private readonly WorldEntities worldEntities;
        private readonly Scene scene;

        private float verticalVelocity;
        private bool jumpQueued;
        private float strideProgress;

        public Pilot(CharacterController controller, Health.Health health, Inventory.Inventory inventory, Speaker.Speaker speaker, Shooter.Shooter shooter, Hands.Hands hands, Clock clock, Sight sight, WorldEntities worldEntities, Scene scene)
        {
            this.controller = controller;
            this.health = health;
            this.inventory = inventory;
            this.speaker = speaker;
            this.shooter = shooter;
            this.hands = hands;

            this.clock = clock;
            this.sight = sight;
            this.worldEntities = worldEntities;
            this.scene = scene;
        }

        public void Apply(PlayerIntent input)
        {
            input.MoveX = Finite(input.MoveX);
            input.MoveZ = Finite(input.MoveZ);
            input.Yaw = Finite(input.Yaw);
            input.Pitch = Finite(input.Pitch);
            LastInput = input;

            if (!health.Alive)
            {
                if (input.Use || input.Jump)
                {
                    Resurrect();
                }

                return;
            }

            if (Sleeping)
            {
                if ((input.Use || input.Jump) && !worldEntities.AllAsleep()) WakeUp();
                return;
            }

            if (input.Jump) jumpQueued = true;

            if (input.Use)
            {
                // TODO remove this shit, damage test
                health.Damage(10);
                if (TryToSleep())
                {
                    return;
                }
            }

            if (input.Shoot)
            {
                TryToShoot();
            }

            if (input.Reload)
            {
                TryToReload();
            }
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
            TickMovement(dt);
        }

        private void TickMovement(float dt)
        {
            if (Sleeping || !health.Alive) return;

            Transform body = controller.transform;
            body.rotation = Quaternion.Euler(0f, LastInput.Yaw, 0f);

            var direction = Vector3.ClampMagnitude(body.right * LastInput.MoveX + body.forward * LastInput.MoveZ, 1f);
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

        private void Resurrect()
        {
            Log.Info("Pilot at {} will be resurrected", controller.transform.position);

            worldEntities.Add(NpcSpawner.Spawn(new Nameable.Nameable(NameableType.SpecialDeadPlayer), new DeadHealth(), new Inventory.Inventory(inventory), controller.transform.position, scene));

            controller.enabled = false;
            controller.transform.position = spawnPoint;
            controller.enabled = true;
            health.Resurrect();
            inventory.Clear();
        }

        private bool TryToSleep()
        {
            if (!hands.Free)
            {
                Log.Info("Pilot tried to sleep with busy hands, ignored");
                return false;
            }
            if (!clock.IsNight())
            {
                Log.Info("Pilot tried to sleep in daytime, ignored");
                return false;
            }
            if (!LookingAtBed())
            {
                Log.Info("Pilot tried to sleep with no bed in sight, ignored");
                return false;
            }
            Sleeping = true;
            spawnPoint = controller.transform.position;
            Log.Info("Pilot fell asleep at {}", controller.transform.position);
            return true;
        }

        private bool TryToShoot()
        {
            return shooter.TryToShoot(controller.transform.position, LastInput.Pitch, LastInput.Yaw);
        }

        private bool TryToReload()
        {
            return shooter.TryToReload();
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
