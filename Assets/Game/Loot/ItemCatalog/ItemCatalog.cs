using System.Collections.Generic;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemSpec[] specs;

        private readonly Dictionary<FixedString32Bytes, ItemSpec> known = new Dictionary<FixedString32Bytes, ItemSpec>();
        private readonly HashSet<FixedString32Bytes> unknown = new HashSet<FixedString32Bytes>();

        public ItemSpec Spec(FixedString32Bytes id)
        {
            if (known.TryGetValue(id, out ItemSpec spec)) return spec;

            if (unknown.Add(id)) Log.Warn("Item catalog {} has no spec for {}", name, id);

            return null;
        }

        public FirearmSpec Firearm(FixedString32Bytes id)
        {
            return Spec(id) as FirearmSpec;
        }

        private void OnEnable()
        {
            known.Clear();
            unknown.Clear();

            if (specs == null) return;

            foreach (ItemSpec spec in specs)
            {
                if (spec == null) continue;

                if (!spec.Fits())
                {
                    Log.Error("Item catalog {} skips {}: its id does not fit the network format", name, spec.name);
                    continue;
                }

                if (known.TryGetValue(spec.Id, out ItemSpec taken))
                {
                    Log.Error("Item catalog {} holds both {} and {} under id {}", name, taken.name, spec.name, spec.Key);
                    continue;
                }

                known.Add(spec.Id, spec);
            }

            Log.Info("Item catalog {} knows {} things", name, known.Count);
        }
    }
}
