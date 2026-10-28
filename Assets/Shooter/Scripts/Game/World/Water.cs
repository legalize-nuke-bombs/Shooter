using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [RequireComponent(typeof(WaterSurface))]
    public class Water : MonoBehaviour
    {
        public float GetDepthAt(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            Vector3 extent = transform.lossyScale / 2f;

            if (Mathf.Abs(offset.x) > extent.x || Mathf.Abs(offset.z) > extent.z) return 0f;

            return Mathf.Max(0f, -offset.y);
        }
    }
}
