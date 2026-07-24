using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
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

        private readonly long userId;
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

        private float moveX;
        private float moveZ;
        private bool sprint;
        private float yaw;
        private float pitch;

        private float verticalVelocity;
        private bool jumpQueued;
        private float strideProgress;
        private bool wasAlive = true;

        public Pilot(long userId, CharacterController controller, Health.Health health, Inventory.Inventory inventory, Speaker.Speaker speaker, Shooter.Shooter shooter, Hands.Hands hands, Clock clock, Sight sight, WorldEntities worldEntities)
        {
            this.userId = userId;
            this.controller = controller;
            this.health = health;
            this.inventory = inventory;
            this.speaker = speaker;
            this.shooter = shooter;
            this.hands = hands;

            this.clock = clock;
            this.sight = sight;
            this.worldEntities = worldEntities;
        }

        public void Apply(PlayerIntent input)
        {
            Steer(input);

            if (!health.Alive)
            {
                ApplyDead(input);
                return;
            }

            if (Sleeping)
            {
                ApplySleeping(input);
                return;
            }

            ApplyAwake(input);
        }

        public void WakeUp()
        {
            if (!Sleeping) return;
            Sleeping = false;
            Log.Info("Pilot at {} woke up", controller.transform.position);
        }

        public override PartState State()
        {
            return new PilotState { UserId = userId, Pitch = pitch, Sleeping = Sleeping };
        }

        public override void Tick(Entity self, float dt)
        {
            if (wasAlive && !health.Alive) hands.Interrupt();
            wasAlive = health.Alive;

            TickMovement(dt);
        }

        private void Steer(PlayerIntent input)
        {
            moveX = Finite(input.MoveX);
            moveZ = Finite(input.MoveZ);
            sprint = input.Sprint;
            yaw = Finite(input.Yaw);
            pitch = Finite(input.Pitch);
        }

        private void ApplyDead(PlayerIntent input)
        {
            if (input.Use || input.Jump)
            {
                Resurrect();
            }
        }

        private void ApplySleeping(PlayerIntent input)
        {
            if ((input.Use || input.Jump) && !worldEntities.AllAsleep())
            {
                WakeUp();
            }
        }

        private void ApplyAwake(PlayerIntent input)
        {
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

            if (!string.IsNullOrEmpty(input.Speech))
            {
                TryToTalk(input.Speech);
            }
        }

        private void TickMovement(float dt)
        {
            if (Sleeping || !health.Alive) return;

            Transform body = controller.transform;
            body.rotation = Quaternion.Euler(0f, yaw, 0f);

            var direction = Vector3.ClampMagnitude(body.right * moveX + body.forward * moveZ, 1f);
            float speed = sprint ? SprintSpeed : WalkSpeed;

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

            {
                var npc = new Entity("Npc", controller.transform.position);
                npc.Body.AddComponent<CapsuleCollider>();
                npc.Add(new Nameable.Nameable(NameableType.SpecialDeadPlayer));
                npc.Add(new DeadHealth());
                npc.Add(new Inventory.Inventory(inventory));
                worldEntities.Add(npc);
            }

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
            return shooter.TryToShoot(controller.transform.position, pitch, yaw);
        }

        private bool TryToReload()
        {
            return shooter.TryToReload();
        }

        private bool TryToTalk(string speech)
        {
            RaycastHit lookHit = LookHit(Talker.Talker.TalkReach);

            if (lookHit.collider == null)
            {
                Log.Info("Pilot tried to talk with no visible target, ignored");
                return false;
            }

            EntityBody entityBody = lookHit.collider.GetComponentInChildren<EntityBody>();
            if (entityBody == null)
            {
                Log.Info("Pilot tried to talk with game object with no entity body component, ignored");
                return false;
            }

            Entity entity = worldEntities.ById(entityBody.Id);
            if (entity == null)
            {
                Log.Warn("Failed to find entity by id encoded in entity body {}", entityBody.Id);
                return false;
            }

            Talker.Talker talker = entity.Get<Talker.Talker>();
            if (talker == null)
            {
                Log.Info("Pilot tried to talk with an entity that is not a talker, ignored");
                return false;
            }

            return talker.TryToListen(userId, speech);
        }

        private bool LookingAtBed()
        {
            RaycastHit lookHit = LookHit(Sleep.UseReach);
            return (lookHit.collider != null && Sleep.IsBed(lookHit));
        }

        private RaycastHit LookHit(float reach)
        {
            Ray ray = Sight.LookRay(controller.transform.position, pitch, yaw);
            sight.Cast(ray, reach, out RaycastHit hit);
            return hit;
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }
    }
}
