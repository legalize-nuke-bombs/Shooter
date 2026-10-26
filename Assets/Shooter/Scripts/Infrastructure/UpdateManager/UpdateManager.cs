using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace Shooter.Infrastructure
{
    public class UpdateManager : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, ProfilerMarker> MarkersByType = new();

        protected override void ShooterAwake()
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

        private static void TellAll<T>(Action<T> tell) where T : class, IShooterBehaviour
        {
            for (int i = Registers.GetAll<T>(RegisterInactivePolicy.Exclude).Length - 1; i >= 0; i--)
            {
                ReadOnlySpan<T> handlers = Registers.GetAll<T>(RegisterInactivePolicy.Exclude);
                if (i < handlers.Length) Tell(handlers[i], tell);
            }
        }

        private static void Tell<T>(T handler, Action<T> tell) where T : class, IShooterBehaviour
        {
#if ENABLE_PROFILER
            ProfilerMarker marker = GetMarker(handler.GetType());
            marker.Begin(handler.NativeBehaviour);
#endif
            try
            {
                tell(handler);
            }
            catch (Exception e)
            {
                Log.Error($"{handler.GetType().Name} {handler.NativeBehaviour.name} failed its update: {e}");
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
