using UnityEngine;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Nameable;

namespace Shooter.Client.Worlds.Entities
{
    public class EntityView
    {
        private const float LerpFactor = 15f;

        public string Name { get; private set; }

        private readonly Transform body;
        private Vector3 targetPosition;
        private float targetYaw;
        private bool sleeping;

        public EntityView(EntityState state)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Entity_" + state.Id;
            capsule.transform.position = new Vector3(state.X, state.Y, state.Z);
            bool piloted = state.Part<PilotState>() != null;
            capsule.GetComponent<Renderer>().material.color =
                piloted ? new Color(0.9f, 0.4f, 0.3f) : new Color(0.5f, 0.55f, 0.5f);
            EntityBody.Attach(capsule, this);
            body = capsule.transform;
            targetPosition = capsule.transform.position;
            Apply(state);
        }

        public void Apply(EntityState state)
        {
            targetPosition = new Vector3(state.X, state.Y, state.Z);
            targetYaw = state.Yaw;
            NameState name = state.Part<NameState>();
            Name = name == null ? "" : name.Name;
            PilotState pilot = state.Part<PilotState>();
            sleeping = pilot != null && pilot.Sleeping;
        }

        public void Tick(float dt)
        {
            float t = 1f - Mathf.Exp(-LerpFactor * dt);
            Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
            if (sleeping) targetRotation *= Quaternion.Euler(0f, 0f, 90f);
            body.position = Vector3.Lerp(body.position, targetPosition, t);
            body.rotation = Quaternion.Slerp(body.rotation, targetRotation, t);
        }

        public void Destroy()
        {
            Object.Destroy(body.gameObject);
        }
    }
}
