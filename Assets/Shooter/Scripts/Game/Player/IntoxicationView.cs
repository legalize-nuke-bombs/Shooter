using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Intoxication))]
    public class IntoxicationView : NetworkBehaviour
    {
        private readonly Dictionary<ToxinAsset, List<PerceptionEffect>> trips = new();

        private Intoxication intoxication;

        private void Awake()
        {
            intoxication = GetComponent<Intoxication>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            ToxinCatalog toxins = Catalogs.Of<ToxinCatalog>();

            for (int index = 0; index < toxins.Count; index++)
            {
                ToxinAsset toxin = toxins.At(index);
                float strength = (float)(intoxication.Level(toxin) / 100d);

                if (strength <= 0f)
                {
                    if (trips.ContainsKey(toxin)) End(toxin);
                    continue;
                }

                if (!trips.TryGetValue(toxin, out List<PerceptionEffect> trip))
                {
                    trip = Begin(toxin);
                    trips.Add(toxin, trip);
                }

                foreach (PerceptionEffect effect in trip) effect.Tick(strength);
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (List<PerceptionEffect> trip in trips.Values)
            foreach (PerceptionEffect effect in trip)
                effect.End();

            trips.Clear();
        }

        private List<PerceptionEffect> Begin(ToxinAsset toxin)
        {
            var trip = new List<PerceptionEffect>();

            foreach (PerceptionEffectAsset asset in toxin.PerceptionEffects)
            {
                if (asset == null) continue;

                trip.Add(asset.Create());
            }

            return trip;
        }

        private void End(ToxinAsset toxin)
        {
            foreach (PerceptionEffect effect in trips[toxin]) effect.End();

            trips.Remove(toxin);
        }
    }
}
