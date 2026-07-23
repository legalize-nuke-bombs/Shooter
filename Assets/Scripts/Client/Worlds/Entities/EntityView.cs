using UnityEngine;
using Shooter.Client.Sounds;
using Shooter.Client.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Speaker;
using Shooter.Server.Worlds.Entities.Parts.Health;

namespace Shooter.Client.Worlds.Entities
{
    public class EntityView
    {
        private const float LerpFactor = 15f;

        public string Name { get; private set; }
        public EntityState State { get; private set; }

        private readonly Transform body;
        private readonly SpeakerView speaker;
        private Vector3 targetPosition;
        private float targetYaw;
        private bool sleeping;
        private bool dead;

        private readonly NameMapper nameMapper = new NameMapper();

        public EntityView(EntityState state)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Entity_" + state.Id;
            capsule.transform.position = new Vector3(state.X, state.Y, state.Z);
            bool piloted = state.Part<PilotState>() != null;
            capsule.GetComponent<Renderer>().material.color =
                piloted ? new Color(0.9f, 0.4f, 0.3f) : new Color(0.5f, 0.55f, 0.5f);
            EntityBody.Attach(capsule, this);
            speaker = new SpeakerView(capsule);
            body = capsule.transform;
            targetPosition = capsule.transform.position;
            Apply(state);
        }

        public void Apply(EntityState state)
        {
            State = state;
            targetPosition = new Vector3(state.X, state.Y, state.Z);
            targetYaw = state.Yaw;

            NameableState nameable = state.Part<NameableState>();
            Name = nameMapper.NameOf(nameable);

            PilotState pilot = state.Part<PilotState>();
            sleeping = pilot != null && pilot.Sleeping;

            HealthState health = state.Part<HealthState>();
            dead = health != null && (health.Hp == 0);

            speaker.Apply(state.Part<SpeakerState>());
        }

        public void Tick(float dt)
        {
            float t = 1f - Mathf.Exp(-LerpFactor * dt);
            var targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
            if (sleeping || dead) targetRotation *= Quaternion.Euler(0f, 0f, 90f);
            body.position = Vector3.Lerp(body.position, targetPosition, t);
            body.rotation = Quaternion.Slerp(body.rotation, targetRotation, t);
        }

        public void Destroy()
        {
            Object.Destroy(body.gameObject);
        }
    }
}
