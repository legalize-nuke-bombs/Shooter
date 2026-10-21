using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    public class ScareFlocksOnShot : ShooterBehaviour
    {
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
            foreach (Flocks flocks in Registers.GetAll<Flocks>(RegisterInactivePolicy.Exclude))
            {
                flocks.ScareRpc(transform.position, asset.ShotSound.MaxDistance);
            }
        }
    }
}
