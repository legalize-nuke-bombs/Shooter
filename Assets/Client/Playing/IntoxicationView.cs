using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Body.Perception;
using Shooter.Game.World;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Intoxication))]
    public class IntoxicationView : NetworkBehaviour
    {
        private Intoxication intoxication;
        private readonly Dictionary<ToxinSpec, List<PerceptionEffect>> trips =
            new Dictionary<ToxinSpec, List<PerceptionEffect>>();

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
                ToxinSpec toxin = toxins.At(index);
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

        private List<PerceptionEffect> Begin(ToxinSpec toxin)
        {
            var trip = new List<PerceptionEffect>();

            foreach (PerceptionEffectSpec spec in toxin.PerceptionEffects)
            {
                if (spec == null) continue;

                trip.Add(spec.Create());
            }

            return trip;
        }

        private void End(ToxinSpec toxin)
        {
            foreach (PerceptionEffect effect in trips[toxin]) effect.End();

            trips.Remove(toxin);
        }
    }
}
