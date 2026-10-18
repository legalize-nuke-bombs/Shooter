using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class Catalog : ScriptableObject
    {
    }

    public abstract class Catalog<TSpec> : Catalog where TSpec : Spec
    {
        public const string SpecsField = nameof(specs);
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private TSpec[] specs;
        private readonly Dictionary<FixedString32Bytes, int> indices = new();
        private readonly List<TSpec> ordered = new();
        private readonly HashSet<FixedString32Bytes> reportedUnknown = new();

        public int Count => ordered.Count;

        protected virtual void OnEnable()
        {
            indices.Clear();
            ordered.Clear();
            reportedUnknown.Clear();

            if (specs == null) return;

            foreach (TSpec spec in specs)
            {
                if (spec == null) continue;

                if (!spec.IdFitsNetwork())
                {
                    Log.Error($"Catalog {name} skips {spec.name}: its id does not fit the network format");
                    continue;
                }

                if (indices.TryGetValue(spec.NetworkId, out int taken))
                {
                    Log.Error($"Catalog {name} holds both {ordered[taken].name} and {spec.name} under id {spec.Id}");
                    continue;
                }

                indices.Add(spec.NetworkId, ordered.Count);
                ordered.Add(spec);
            }

            Log.Info($"Catalog {name} knows {ordered.Count} things");
        }

        public TSpec At(int index)
        {
            return index >= 0 && index < ordered.Count ? ordered[index] : null;
        }

        public int Index(TSpec spec)
        {
            return spec != null && indices.TryGetValue(spec.NetworkId, out int index) ? index : -1;
        }

        public TSpec Of(FixedString32Bytes id)
        {
            if (indices.TryGetValue(id, out int index)) return ordered[index];

            if (reportedUnknown.Add(id))
            {
                Log.Warn($"Catalog {name} has nothing under id {id}");
            }

            return null;
        }

        public List<TSpec> FindAll(Predicate<TSpec> predicate)
        {
            return ordered.FindAll(predicate);
        }
    }
}
