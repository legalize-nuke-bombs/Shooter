using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.World;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Intoxication))]
    public class IntoxicationView : NetworkBehaviour
    {
        private Intoxication intoxication;
        private readonly Dictionary<ToxinSpec, List<PerceptionEffect>> trips =
            new Dictionary<ToxinSpec, List<PerceptionEffect>>();
        private readonly List<ToxinSpec> ended = new List<ToxinSpec>();

        public Vector3 CameraSway { get; set; }

        private void Awake()
        {
            intoxication = GetComponent<Intoxication>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            CameraSway = Vector3.zero;

            ToxinCatalog toxins = Environment.Current.Toxins;

            for (int index = 0; index < toxins.Count; index++)
            {
                ToxinSpec toxin = toxins.At(index);
                float strength = (float)(intoxication.Level(toxin) / 100d);

                if (strength <= 0f)
                {
                    if (trips.ContainsKey(toxin)) ended.Add(toxin);
                    continue;
                }

                if (!trips.TryGetValue(toxin, out List<PerceptionEffect> trip))
                {
                    trip = Begin(toxin);
                    trips.Add(toxin, trip);
                }

                foreach (PerceptionEffect effect in trip) effect.Tick(strength);
            }

            foreach (ToxinSpec toxin in ended) End(toxin);
            ended.Clear();
        }

        public override void OnNetworkDespawn()
        {
            foreach (List<PerceptionEffect> trip in trips.Values)
                foreach (PerceptionEffect effect in trip)
                    effect.End();

            trips.Clear();
            CameraSway = Vector3.zero;
        }

        private static List<PerceptionEffect> Begin(ToxinSpec toxin)
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
