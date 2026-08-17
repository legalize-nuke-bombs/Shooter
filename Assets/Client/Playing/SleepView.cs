using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    public class SleepView : NetworkBehaviour
    {
        private const float EyesClose = 0.45f;
        private const float EyesOpen = 0.7f;
        private const float BedReach = 2f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera view;
        private Camera bedside;

        private Sleeper sleeper;
        private bool watching;

        public float Blink { get; private set; }

        private void Awake()
        {
            sleeper = this.Find<Sleeper>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || view == null) return;

            bool asleep = sleeper.Sleeping;

            if (asleep != watching)
            {
                Blink = Mathf.MoveTowards(Blink, 1f, Time.deltaTime / EyesClose);
                if (Blink < 1f) return;

                watching = asleep;
                if (asleep) Watch();
                else Wake();

                return;
            }

            float open = asleep && bedside == null ? 1f : 0f;

            Blink = Mathf.MoveTowards(Blink, open, Time.deltaTime / EyesOpen);
        }

        public override void OnNetworkDespawn()
        {
            if (watching) Wake();

            Blink = 0f;
        }

        private void Watch()
        {
            bedside = Nearest();

            if (bedside == null)
            {
                Log.Warn($"Bed at {sleeper.Bedside} has no camera of its own, the night stays dark");
                return;
            }

            bedside.enabled = true;
            view.enabled = false;
        }

        private void Wake()
        {
            if (bedside != null) bedside.enabled = false;

            bedside = null;
            view.enabled = true;
        }

        private Camera Nearest()
        {
            Vector3 slept = sleeper.Bedside;
            Bed closest = null;
            float best = BedReach * BedReach;

            foreach (Bed bed in FindObjectsByType<Bed>())
            {
                float apart = (bed.transform.position - slept).sqrMagnitude;
                if (apart > best) continue;

                best = apart;
                closest = bed;
            }

            return closest == null ? null : closest.Bedside;
        }
    }
}
