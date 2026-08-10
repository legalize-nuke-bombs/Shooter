using Shooter.Game.Body.Sleeping;
using Shooter.Logging;
using UnityEngine;
using Unity.Netcode;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Sleeper))]
    public class SleepView : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private const float EyesClose = 0.45f;
        private const float EyesOpen = 0.7f;
        private const float BedReach = 2f;

        [SerializeField] private Camera view;

        private Sleeper sleeper;
        private Camera bedside;
        private float blink;
        private bool watching;

        public float Blink => blink;

        private void Awake()
        {
            sleeper = GetComponent<Sleeper>();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || view == null) return;

            bool asleep = sleeper.Sleeping;

            if (asleep != watching)
            {
                blink = Mathf.MoveTowards(blink, 1f, Time.deltaTime / EyesClose);
                if (blink < 1f) return;

                watching = asleep;
                if (asleep) Watch();
                else Wake();

                return;
            }

            float open = asleep && bedside == null ? 1f : 0f;

            blink = Mathf.MoveTowards(blink, open, Time.deltaTime / EyesOpen);
        }

        public override void OnNetworkDespawn()
        {
            if (watching) Wake();

            blink = 0f;
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
