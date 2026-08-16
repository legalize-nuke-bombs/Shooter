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
        private ToxinSpec dominant;
        private readonly List<PerceptionEffectInstance> trip = new List<PerceptionEffectInstance>();

        public Vector3 CameraSway { get; set; }

        private void Awake()
        {
            intoxication = GetComponent<Intoxication>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            ToxinSpec strongest = Strongest();

            if (strongest != dominant) Switch(strongest);

            if (dominant == null) return;

            float strength = (float)(intoxication.Level(dominant) / 100d);

            foreach (PerceptionEffectInstance effect in trip) effect.Tick(strength);
        }

        public override void OnNetworkDespawn()
        {
            Switch(null);
        }

        private ToxinSpec Strongest()
        {
            ToxinCatalog toxins = Environment.Current.Toxins;

            ToxinSpec strongest = null;
            double highest = 0;

            for (int index = 0; index < toxins.Count; index++)
            {
                ToxinSpec toxin = toxins.At(index);
                double level = intoxication.Level(toxin);

                if (level <= highest) continue;

                highest = level;
                strongest = toxin;
            }

            return strongest;
        }

        private void Switch(ToxinSpec next)
        {
            foreach (PerceptionEffectInstance effect in trip) effect.End();
            trip.Clear();

            CameraSway = Vector3.zero;
            dominant = next;

            if (next == null) return;

            foreach (PerceptionEffect effect in next.PerceptionEffects)
            {
                if (effect == null) continue;

                trip.Add(effect.Begin());
            }
        }
    }
}
