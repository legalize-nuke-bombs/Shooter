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
            int i = 0;
            ReadOnlySpan<T> handlers = Registers.GetAll<T>(RegisterInactivePolicy.Exclude);
            while (i < handlers.Length)
            {
                T handler = handlers[i];
                Tell(handler, tell);
                handlers = Registers.GetAll<T>(RegisterInactivePolicy.Exclude);
                // A handler that switched itself off moved the ones after it a slot back, so the same slot is told next
                if (i < handlers.Length && handlers[i] == handler) i++;
            }
        }

        // As with Update called by Unity, an exception stops only the handler that threw it
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

        // Without a marker per type the profiler would show every update as one UpdateManager.Update
        private static ProfilerMarker GetMarker(Type type)
        {
            if (MarkersByType.TryGetValue(type, out ProfilerMarker marker)) return marker;

            marker = new ProfilerMarker(type.Name);
            MarkersByType[type] = marker;
            return marker;
        }
    }
}
