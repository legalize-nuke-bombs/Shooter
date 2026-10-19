using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Intoxication))]
    public class IntoxicationView : NetworkBehaviour
    {
        private readonly Dictionary<ToxinAsset, List<PerceptionEffect>> perceptionEffectsByToxin = new();

        private Intoxication intoxication;

        private void Awake()
        {
            intoxication = GetComponent<Intoxication>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            ToxinCatalog toxinCatalog = Catalogs.GetCatalog<ToxinCatalog>();

            for (int index = 0; index < toxinCatalog.Count; index++)
            {
                ToxinAsset toxin = toxinCatalog.GetAssetAt(index);
                float strength = (float)(intoxication.GetLevel(toxin) / 100d);

                if (strength <= 0f)
                {
                    if (perceptionEffectsByToxin.ContainsKey(toxin)) EndPerceptionEffects(toxin);
                    continue;
                }

                if (!perceptionEffectsByToxin.TryGetValue(toxin, out List<PerceptionEffect> perceptionEffects))
                {
                    perceptionEffects = CreatePerceptionEffects(toxin);
                    perceptionEffectsByToxin.Add(toxin, perceptionEffects);
                }

                foreach (PerceptionEffect perceptionEffect in perceptionEffects)
                {
                    perceptionEffect.UpdateStrength(strength);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (List<PerceptionEffect> perceptionEffects in perceptionEffectsByToxin.Values)
            {
                foreach (PerceptionEffect perceptionEffect in perceptionEffects)
                {
                    perceptionEffect.EndEffect();
                }
            }

            perceptionEffectsByToxin.Clear();
        }

        private List<PerceptionEffect> CreatePerceptionEffects(ToxinAsset toxin)
        {
            var perceptionEffects = new List<PerceptionEffect>();

            foreach (PerceptionEffectAsset perceptionEffectAsset in toxin.PerceptionEffects)
            {
                if (perceptionEffectAsset == null) continue;

                perceptionEffects.Add(perceptionEffectAsset.CreateEffect());
            }

            return perceptionEffects;
        }

        private void EndPerceptionEffects(ToxinAsset toxin)
        {
            foreach (PerceptionEffect perceptionEffect in perceptionEffectsByToxin[toxin])
            {
                perceptionEffect.EndEffect();
            }

            perceptionEffectsByToxin.Remove(toxin);
        }
    }
}
