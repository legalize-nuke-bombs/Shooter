using UnityEngine;
using Shooter.Server.Worlds.Entities.Players;

namespace Shooter.Client.Entities.Players
{
    public class PlayerAvatar
    {
        private const float LerpFactor = 15f;

        private readonly Transform body;
        private Vector3 targetPosition;
        private float targetYaw;
        private bool sleeping;

        public PlayerAvatar(long id, Vector3 position)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Player_" + id;
            capsule.transform.position = position;
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);
            body = capsule.transform;
            targetPosition = position;
        }

        public void Destroy()
        {
            Object.Destroy(body.gameObject);
        }

        public void Apply(PlayerState state)
        {
            targetPosition = new Vector3(state.X, state.Y, state.Z);
            targetYaw = state.Yaw;
            sleeping = state.Sleeping;
        }

        public void Interpolate(float dt)
        {
            float t = 1f - Mathf.Exp(-LerpFactor * dt);
            var targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
            if (sleeping) targetRotation *= Quaternion.Euler(0f, 0f, 90f);
            body.position = Vector3.Lerp(body.position, targetPosition, t);
            body.rotation = Quaternion.Slerp(body.rotation, targetRotation, t);
        }
    }
}
