using System;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly TMap Behaviours = new();
        private static readonly TMap ActiveBehaviours = new();

        public static void RegisterBehaviour(IShooterBehaviour behaviour)
        {
            Behaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} registered");
        }

        public static void UnregisterBehaviour(IShooterBehaviour behaviour)
        {
            Behaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} unregistered");
        }

        public static void ActivateBehaviour(IShooterBehaviour behaviour)
        {
            ActiveBehaviours.Add(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} activated");
        }

        public static void DeactivateBehaviour(IShooterBehaviour behaviour)
        {
            ActiveBehaviours.Remove(behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} deactivated");
        }

        public static ReadOnlySpan<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            TMap behaviours = inactivePolicy == RegisterInactivePolicy.Include ? Behaviours : ActiveBehaviours;
            return behaviours.GetAll<T>();
        }

        public static T Get<T>() where T : class
        {
            ReadOnlySpan<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Length != 1) throw new InvalidOperationException($"{members.Length} {typeof(T).Name} registered instead of one");
            return members[0];
        }
    }
}
