using System;
using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public static class Registers
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Dictionary<Type, Register> RegistersByType = new();
        private static readonly Dictionary<Type, Register> ActiveRegistersByType = new();
        private static readonly Dictionary<Type, Type[]> RegisteredTypesByBehaviourType = new();

        public static void RegisterBehaviour(IShooterBehaviour behaviour)
        {
            AddToRegisters(RegistersByType, behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} registered");
        }

        public static void UnregisterBehaviour(IShooterBehaviour behaviour)
        {
            RemoveFromRegisters(RegistersByType, behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} unregistered");
        }

        public static void ActivateBehaviour(IShooterBehaviour behaviour)
        {
            AddToRegisters(ActiveRegistersByType, behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} activated");
        }

        public static void DeactivateBehaviour(IShooterBehaviour behaviour)
        {
            RemoveFromRegisters(ActiveRegistersByType, behaviour);
            Log.Debug($"{behaviour.GetType().Name} {behaviour.NativeBehaviour.name} deactivated");
        }

        public static ReadOnlySpan<T> GetAll<T>(RegisterInactivePolicy inactivePolicy) where T : class
        {
            Dictionary<Type, Register> registersByType = inactivePolicy == RegisterInactivePolicy.Include ? RegistersByType : ActiveRegistersByType;
            return registersByType.TryGetValue(typeof(T), out Register register) ? register.GetMembers<T>() : ReadOnlySpan<T>.Empty;
        }

        public static T Get<T>() where T : class
        {
            ReadOnlySpan<T> members = GetAll<T>(RegisterInactivePolicy.Include);
            if (members.Length != 1) throw new InvalidOperationException($"{members.Length} {typeof(T).Name} registered instead of one");
            return members[0];
        }

        private static void AddToRegisters(Dictionary<Type, Register> registersByType, IShooterBehaviour behaviour)
        {
            foreach (Type type in GetRegisteredTypes(behaviour.GetType()))
            {
                if (!registersByType.TryGetValue(type, out Register register))
                {
                    register = new Register(type);
                    registersByType[type] = register;
                }

                register.AddMember(behaviour);
            }
        }

        private static void RemoveFromRegisters(Dictionary<Type, Register> registersByType, IShooterBehaviour behaviour)
        {
            foreach (Type type in GetRegisteredTypes(behaviour.GetType()))
            {
                if (registersByType.TryGetValue(type, out Register register)) register.RemoveMember(behaviour);
            }
        }

        // GetInterfaces builds a new array on every call, so the types a behaviour registers under are found once per type
        private static Type[] GetRegisteredTypes(Type behaviourType)
        {
            if (RegisteredTypesByBehaviourType.TryGetValue(behaviourType, out Type[] registeredTypes)) return registeredTypes;

            var types = new List<Type>();
            for (Type type = behaviourType; type != typeof(ShooterBehaviour) && type != typeof(ShooterNetworkBehaviour); type = type.BaseType)
            {
                types.Add(type);
            }

            foreach (Type interfaceType in behaviourType.GetInterfaces())
            {
                if (interfaceType != typeof(IShooterBehaviour)) types.Add(interfaceType);
            }

            registeredTypes = types.ToArray();
            RegisteredTypesByBehaviourType[behaviourType] = registeredTypes;
            return registeredTypes;
        }
    }
}
