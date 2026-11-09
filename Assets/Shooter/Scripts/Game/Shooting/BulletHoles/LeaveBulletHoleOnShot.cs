using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    public class LeaveBulletHoleOnShot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private BulletHoles bulletHoles;

        private void Awake()
        {
            gunner = GetComponent<Gunner>();
            bulletHoles = Registers.Instance.Get<BulletHoles>();
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
            string missingHoleReason = FindMissingHoleReason(hit);
            if (missingHoleReason != null)
            {
                Log.Info($"Entity {name} left no bullet hole: {missingHoleReason}");
                return;
            }

            bulletHoles.AddHole(hit.Nearest.point, hit.Nearest.normal);
            Log.Info($"Entity {name} left a bullet hole on {hit.Nearest.collider.name}");
        }

        private static string FindMissingHoleReason(ShotHit hit)
        {
            if (hit.IsMiss) return "the shot missed";
            if (hit.Nearest.collider.GetComponent<Hitbox>() != null) return $"it hit a body at {hit.Nearest.collider.name}";

            return Surface.IsWater(Surface.FindAt(hit.Nearest)) ? "it hit water" : null;
        }
    }
}
