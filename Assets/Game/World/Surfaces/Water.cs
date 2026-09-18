using Shooter.Logging;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(WaterSurface))]
    public class Water : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static bool missed;

        public static Water Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
            missed = false;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public static float Depth(Vector3 point)
        {
            if (Current == null)
            {
                if (!missed) Log.Warn("The world has no Water component on its water surface, every lake sounds like its bed");
                missed = true;
                return 0f;
            }

            Transform surface = Current.transform;
            Vector3 offset = point - surface.position;
            Vector3 extent = surface.lossyScale / 2f;

            if (Mathf.Abs(offset.x) > extent.x || Mathf.Abs(offset.z) > extent.z) return 0f;

            return Mathf.Max(0f, -offset.y);
        }
    }
}
