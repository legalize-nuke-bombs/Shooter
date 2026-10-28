using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public class UpdateManager : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, ProfilerMarker> MarkersByType = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            TellAll<IShooterUpdateable>(static updateable => updateable.ShooterUpdate());
        }

        private void LateUpdate()
        {
            TellAll<IShooterLateUpdateable>(static updateable => updateable.ShooterLateUpdate());
        }

        private static void TellAll<T>(Action<T> tell) where T : class
        {
            foreach (T handler in Registers.Instance.GetAll<T>(RegisterInactivePolicy.Exclude))
            {
                Tell(handler, tell);
            }
        }

        private static void Tell<T>(T handler, Action<T> tell) where T : class
        {
            var behaviour = (MonoBehaviour)(object)handler;
#if ENABLE_PROFILER
            ProfilerMarker marker = GetMarker(handler.GetType());
            marker.Begin(behaviour);
#endif
            try
            {
                tell(handler);
            }
            catch (Exception e)
            {
                Log.Error($"{handler.GetType().Name} {behaviour.name} failed its update: {e}");
            }
#if ENABLE_PROFILER
            marker.End();
#endif
        }

        private static ProfilerMarker GetMarker(Type type)
        {
            if (MarkersByType.TryGetValue(type, out ProfilerMarker marker)) return marker;

            marker = new ProfilerMarker(type.Name);
            MarkersByType[type] = marker;
            return marker;
        }
    }
}
