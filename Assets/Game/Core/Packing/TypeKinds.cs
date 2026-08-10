using System;
using System.Collections.Generic;
using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Packing
{
    public sealed class TypeKinds<TBase> : IKinds<TBase> where TBase : class, INetworkSerializable
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<int, Type> kinds = new Dictionary<int, Type>();
        private readonly Dictionary<Type, int> tags = new Dictionary<Type, int>();

        public TypeKinds()
        {
            foreach (Type type in typeof(TBase).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(TBase).IsAssignableFrom(type)) continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Log.Error($"Kind {type.FullName} has no parameterless constructor and will never arrive");
                    continue;
                }

                int tag = Tag(type.Name);

                if (kinds.TryGetValue(tag, out Type taken))
                {
                    Log.Error($"Kinds {taken.FullName} and {type.FullName} share the tag {tag}, the second one will never arrive");
                    continue;
                }

                kinds.Add(tag, type);
                tags.Add(type, tag);
            }

            Log.Info($"Kinds of {typeof(TBase).Name} known: {kinds.Count}");
        }

        public int Of(TBase value)
        {
            if (value != null && tags.TryGetValue(value.GetType(), out int tag)) return tag;

            Log.Error($"Kind {(value == null ? "null" : value.GetType().FullName)} is not a known {typeof(TBase).Name}");

            return 0;
        }

        public TBase Create(int kind)
        {
            if (kinds.TryGetValue(kind, out Type known)) return (TBase)Activator.CreateInstance(known);

            Log.Error($"A {typeof(TBase).Name} of unknown kind {kind} arrived");

            return null;
        }

        private static int Tag(string name)
        {
            unchecked
            {
                const int offset = (int)2166136261;
                const int prime = 16777619;

                int hash = offset;
                foreach (char letter in name) hash = (hash ^ letter) * prime;

                return hash;
            }
        }
    }
}
