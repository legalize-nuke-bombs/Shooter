using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    public class LeaveBulletHoleOnShot : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private BulletHoles bulletHoles;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            bulletHoles = Registers.Instance.Get<BulletHoles>();
        }

        protected override void ShooterOnEnable()
        {
            gunner.Shot += LeaveBulletHole;
        }

        protected override void ShooterOnDisable()
        {
            gunner.Shot -= LeaveBulletHole;
        }

        private void LeaveBulletHole(FirearmAsset asset, ShotHit hit)
        {
            string missingHoleReason = FindMissingHoleReason(hit);
            if (missingHoleReason != null)
            {
                Log.Info($"Entity {name} left no bullet hole: {missingHoleReason}");
                return;
            }

            bulletHoles.AddHole(hit.Point, hit.Normal);
            Log.Info($"Entity {name} left a bullet hole on {hit.Collider.name}");
        }

        private static string FindMissingHoleReason(ShotHit hit)
        {
            if (hit.IsMiss) return "the shot missed";
            if (hit.Victim != null) return $"it hit {hit.Victim.name}";

            return Surface.IsWater(hit.Surface) ? "it hit water" : null;
        }
    }
}
