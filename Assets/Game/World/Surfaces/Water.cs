using Shooter.Game.Core;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(WaterSurface))]
    public class Water : RegisteredBehaviour
    {
        // Nobody swims: an invisible wall stands where the water gets this deep and rises this high above the surface.
        // The wall itself is built in the editor, Tools / Build Water Walls
        [SerializeField] private float wallDepth = 1f;
        [SerializeField] private float wallHeight = 100f;

        public float WallDepth => wallDepth;
        public float WallHeight => wallHeight;

        // A map may hold no water or several surfaces: the depth is that of the water the point is under
        public static float Depth(Vector3 point)
        {
            float deepest = 0f;

            foreach (Water water in Registers.Of<Water>(Inactive.Exclude))
                deepest = Mathf.Max(deepest, water.DepthAt(point));

            return deepest;
        }

        private float DepthAt(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            Vector3 extent = transform.lossyScale / 2f;

            if (Mathf.Abs(offset.x) > extent.x || Mathf.Abs(offset.z) > extent.z) return 0f;

            return Mathf.Max(0f, -offset.y);
        }
    }
}
