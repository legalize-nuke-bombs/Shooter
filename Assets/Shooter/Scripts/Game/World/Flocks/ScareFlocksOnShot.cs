using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    public class ScareFlocksOnShot : ShooterBehaviour
    {
        [SerializeField] private float scareRadius = 20f;

        [SerializeField] private float missFlightDistance = 400f;

        private Gunner gunner;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
        }

        private void OnEnable()
        {
            gunner.Shot += Scare;
        }

        private void OnDisable()
        {
            gunner.Shot -= Scare;
        }

        private void Scare(FirearmAsset asset, ShotHit hit)
        {
            Vector3 shotEnd = hit.IsMiss ? hit.Origin + hit.Direction * missFlightDistance : hit.Point;

            foreach (Flocks flocks in Registers.GetAll<Flocks>(RegisterInactivePolicy.Exclude))
            {
                flocks.ScareRpc(hit.Origin, shotEnd, scareRadius);
            }
        }
    }
}
