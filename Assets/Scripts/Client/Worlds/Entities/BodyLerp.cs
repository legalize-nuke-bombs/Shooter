using UnityEngine;

namespace Shooter.Client.Worlds.Entities
{
    public static class BodyLerp
    {
        private const float LerpFactor = 15f;

        public static void Follow(Transform body, Vector3 targetPosition, Quaternion targetRotation, float dt)
        {
            float t = 1f - Mathf.Exp(-LerpFactor * dt);
            body.position = Vector3.Lerp(body.position, targetPosition, t);
            body.rotation = Quaternion.Slerp(body.rotation, targetRotation, t);
        }

        public static void Follow(Transform body, Vector3 targetPosition, float dt)
        {
            body.position = Vector3.Lerp(body.position, targetPosition, 1f - Mathf.Exp(-LerpFactor * dt));
        }
    }
}
