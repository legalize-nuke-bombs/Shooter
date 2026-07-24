using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities.Creating;
using Shooter.Server.Worlds.Entities.Parts.Movement;
using Shooter.Server.Worlds.Entities.Parts.Sleeper;

namespace Shooter.Server.Worlds.Entities.Parts.Pilot
{
    public sealed class Pilot : Part
    {
        private const float WalkSpeed = 5f;
        private const float SprintSpeed = 8f;

        private readonly long userId;
        private readonly Gaze gaze;
        private readonly WorldEntities worldEntities;

        private float pitch;

        public Pilot(Entity self, long userId, Gaze gaze, WorldEntities worldEntities) : base(self, typeof(Pilot))
        {
            this.userId = userId;
            this.gaze = gaze;
            this.worldEntities = worldEntities;
        }

        public long UserId => userId;

        public override void Apply(PlayerIntent input)
        {
            pitch = Finite(input.Pitch);

            Health.Health health = Self.Get<Health.Health>();
            if (health != null && !health.Alive)
            {
                if (input.Use || input.Jump) Resurrect(health);
                return;
            }

            Sleeper.Sleeper sleeper = Self.Get<Sleeper.Sleeper>();
            if (sleeper != null && sleeper.Sleeping) return;

            Steer(input);

            if (input.Use)
            {
                // TODO remove this shit, damage test
                health?.Damage(10);
            }

            if (!string.IsNullOrEmpty(input.Speech)) TryTalk(input.Speech);
        }

        public override PartState State()
        {
            return new PilotState { UserId = userId };
        }

        private void Steer(PlayerIntent input)
        {
            Movement.Movement motion = Self.Get<Movement.Movement>();
            if (motion == null) return;

            motion.Face(Finite(input.Yaw));
            motion.Steer(Finite(input.MoveZ), Finite(input.MoveX), input.Sprint ? SprintSpeed : WalkSpeed);
            if (input.Jump) motion.Jump();
        }

        private void Resurrect(Health.Health health)
        {
            Sleeper.Sleeper sleeper = Self.Get<Sleeper.Sleeper>();
            Vector3 spawnPoint = sleeper == null ? Vector3.zero : sleeper.SpawnPoint;

            Log.Info("Entity {} is resurrecting at {}", Self.Name, spawnPoint);

            worldEntities.Add(CorpseCreator.Create(Self));
            Self.Get<Movement.Movement>()?.Teleport(spawnPoint);
            health.Resurrect();
        }

        private bool TryTalk(string speech)
        {
            if (!gaze.TryLookAt(Self.Position, pitch, Self.Yaw, Talker.Talker.TalkReach, out Entity target))
            {
                Log.Info("Entity {} tried to talk with no entity in sight, ignored", Self.Name);
                return false;
            }

            Talker.Talker talker = target.Get<Talker.Talker>();
            if (talker == null)
            {
                Log.Info("Entity {} tried to talk to entity {} that is not a talker, ignored", Self.Name, target.Name);
                return false;
            }

            return talker.TryListen(Self, speech);
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }
    }
}
