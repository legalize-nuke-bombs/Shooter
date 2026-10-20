using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(Sleeper))]
    public class SleepView : ShooterNetworkBehaviour
    {
        private const float EyesClosingSeconds = 0.45f;
        private const float EyesOpeningSeconds = 0.7f;
        private const float BedReach = 2f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera view;

        private Camera bedsideCamera;
        private Player player;
        private Sleeper sleeper;
        private bool shownAsleep;

        public float EyesClosedAmount { get; private set; }

        protected override void ShooterAwake()
        {
            player = GetComponent<Player>();
            sleeper = GetComponent<Sleeper>();
        }

        private void Update()
        {
            if (!player.IsControlledLocally || view == null) return;

            bool asleep = sleeper.IsSleeping;

            if (asleep != shownAsleep)
            {
                EyesClosedAmount = Mathf.MoveTowards(EyesClosedAmount, 1f, Time.deltaTime / EyesClosingSeconds);
                if (EyesClosedAmount < 1f) return;

                shownAsleep = asleep;
                if (asleep)
                {
                    SwitchToBedsideCamera();
                }
                else
                {
                    SwitchToPlayerCamera();
                }

                return;
            }

            float targetEyesClosedAmount = asleep && bedsideCamera == null ? 1f : 0f;

            EyesClosedAmount = Mathf.MoveTowards(EyesClosedAmount, targetEyesClosedAmount, Time.deltaTime / EyesOpeningSeconds);
        }

        public override void OnNetworkDespawn()
        {
            if (shownAsleep) SwitchToPlayerCamera();

            EyesClosedAmount = 0f;
        }

        private void SwitchToBedsideCamera()
        {
            bedsideCamera = FindNearestBedsideCamera();

            if (bedsideCamera == null)
            {
                Log.Warn($"Bed at {sleeper.BedPosition} has no camera of its own, the night stays dark");
                return;
            }

            bedsideCamera.enabled = true;
            view.enabled = false;
        }

        private void SwitchToPlayerCamera()
        {
            if (bedsideCamera != null) bedsideCamera.enabled = false;

            bedsideCamera = null;
            view.enabled = true;
        }

        private Camera FindNearestBedsideCamera()
        {
            Vector3 bedPosition = sleeper.BedPosition;
            Bed closestBed = null;
            float closestSquaredDistance = BedReach * BedReach;

            foreach (Bed bed in FindObjectsByType<Bed>())
            {
                float squaredDistance = (bed.transform.position - bedPosition).sqrMagnitude;
                if (squaredDistance > closestSquaredDistance) continue;

                closestSquaredDistance = squaredDistance;
                closestBed = bed;
            }

            return closestBed == null ? null : closestBed.BedsideCamera;
        }
    }
}
