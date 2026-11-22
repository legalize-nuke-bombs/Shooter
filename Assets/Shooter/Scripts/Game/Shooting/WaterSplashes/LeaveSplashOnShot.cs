using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    public class LeaveSplashOnShot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;

        private void Awake()
        {
            gunner = GetComponent<Gunner>();
        }

        private void OnEnable()
        {
            gunner.Shot += LeaveSplash;
        }

        private void OnDisable()
        {
            gunner.Shot -= LeaveSplash;
        }

        private void LeaveSplash(FirearmAsset asset, ShotHit hit)
        {
            if (hit.IsMiss || !Surface.IsWater(Surface.FindAt(hit.Nearest))) return;

            Registers.Instance.Get<WaterSplashes>().LeaveSplash(hit.Nearest.point, hit.Ray.direction);
            Log.Info($"Entity {name} left a splash on the water at {hit.Nearest.point}");
        }
    }
}
