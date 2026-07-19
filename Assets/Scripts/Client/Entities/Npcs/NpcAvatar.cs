using UnityEngine;
using Shooter.Server.Worlds.Entities.Npcs;

namespace Shooter.Client.Entities.Npcs
{
    public class NpcAvatar
    {
        private const float LerpFactor = 15f;

        public string Name { get; private set; }

        private readonly Transform body;
        private Vector3 targetPosition;
        private float targetYaw;

        public NpcAvatar(long id, Vector3 position)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Npc_" + id;
            capsule.transform.position = position;
            capsule.GetComponent<Renderer>().material.color = new Color(0.5f, 0.55f, 0.5f);
            NpcBody.Attach(capsule, this);
            body = capsule.transform;
            targetPosition = position;
        }

        public void Destroy()
        {
            Object.Destroy(body.gameObject);
        }

        public void Apply(NpcState state)
        {
            Name = state.Name;
            targetPosition = new Vector3(state.X, state.Y, state.Z);
            targetYaw = state.Yaw;
        }

        public void Interpolate(float dt)
        {
            float t = 1f - Mathf.Exp(-LerpFactor * dt);
            body.position = Vector3.Lerp(body.position, targetPosition, t);
            body.rotation = Quaternion.Slerp(body.rotation, Quaternion.Euler(0f, targetYaw, 0f), t);
        }
    }
}
