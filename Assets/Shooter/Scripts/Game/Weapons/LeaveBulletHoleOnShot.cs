using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    public class LeaveBulletHoleOnShot : ShooterBehaviour
    {
        private Gunner gunner;
        private BulletHoles bulletHoles;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            bulletHoles = Registers.Get<BulletHoles>();
        }

        private void OnEnable()
        {
            gunner.Shot += LeaveBulletHole;
        }

        private void OnDisable()
        {
            gunner.Shot -= LeaveBulletHole;
        }

        private void LeaveBulletHole(FirearmAsset asset, ShotHit hit)
        {
            if (hit.IsMiss || hit.Victim != null) return;

            bulletHoles.AddHole(hit.Point, hit.Normal);
        }
    }
}
