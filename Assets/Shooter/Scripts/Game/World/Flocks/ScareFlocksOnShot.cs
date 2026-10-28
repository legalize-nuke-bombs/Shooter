using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    public class ScareFlocksOnShot : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float scareRadius = 20f;

        [SerializeField] private float missFlightDistance = 400f;

        private Gunner gunner;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
        }

        protected override void ShooterOnEnable()
        {
            gunner.Shot += Scare;
        }

        protected override void ShooterOnDisable()
        {
            gunner.Shot -= Scare;
        }

        private void Scare(FirearmAsset asset, ShotHit hit)
        {
            Vector3 shotEnd = hit.IsMiss ? hit.Origin + hit.Direction * missFlightDistance : hit.Point;

            int flocksCount = 0;
            foreach (Flocks flocks in Registers.Instance.GetAll<Flocks>(RegisterInactivePolicy.Exclude))
            {
                flocks.ScareRpc(hit.Origin, shotEnd, scareRadius);
                flocksCount++;
            }

            Log.Info($"Entity {name} sent the {Vector3.Distance(hit.Origin, shotEnd):0} m line of its shot to {flocksCount} bird flocks");
        }
    }
}
