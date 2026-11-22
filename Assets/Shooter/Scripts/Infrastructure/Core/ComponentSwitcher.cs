using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public static class ComponentSwitcher
    {
        private static readonly ConditionalWeakTable<WindZone, WindStrength> CalmedWinds = new();

        public static bool CanSwitch(Component component)
        {
            return component is Behaviour or Renderer or Collider or LODGroup or WindZone;
        }

        public static bool IsEnabled(Component component)
        {
            return component switch
            {
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                Collider collider => collider.enabled,
                LODGroup lodGroup => lodGroup.enabled,
                WindZone wind => !CalmedWinds.TryGetValue(wind, out _),
                _ => throw new ArgumentException($"{component.GetType().Name} can't be switched")
            };
        }

        public static void SetEnabled(Component component, bool componentEnabled)
        {
            switch (component)
            {
                case Behaviour behaviour:
                    behaviour.enabled = componentEnabled;
                    break;
                case Renderer renderer:
                    renderer.enabled = componentEnabled;
                    break;
                case Collider collider:
                    collider.enabled = componentEnabled;
                    break;
                case LODGroup lodGroup:
                    lodGroup.enabled = componentEnabled;
                    break;
                case WindZone wind:
                    if (componentEnabled) Restore(wind);
                    else Calm(wind);
                    break;
                default:
                    throw new ArgumentException($"{component.GetType().Name} can't be switched");
            }
        }

        private static void Calm(WindZone wind)
        {
            if (CalmedWinds.TryGetValue(wind, out _)) return;

            CalmedWinds.Add(wind, new WindStrength(wind.windMain, wind.windTurbulence, wind.windPulseMagnitude));
            wind.windMain = 0f;
            wind.windTurbulence = 0f;
            wind.windPulseMagnitude = 0f;
        }

        private static void Restore(WindZone wind)
        {
            if (!CalmedWinds.TryGetValue(wind, out WindStrength strength)) return;

            CalmedWinds.Remove(wind);
            wind.windMain = strength.Main;
            wind.windTurbulence = strength.Turbulence;
            wind.windPulseMagnitude = strength.PulseMagnitude;
        }

        private sealed class WindStrength
        {
            public readonly float Main;
            public readonly float Turbulence;
            public readonly float PulseMagnitude;

            public WindStrength(float main, float turbulence, float pulseMagnitude)
            {
                Main = main;
                Turbulence = turbulence;
                PulseMagnitude = pulseMagnitude;
            }
        }
    }
}
