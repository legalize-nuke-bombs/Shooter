using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    public class ScareFlocksOnShot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float scareRadius = 20f;

        [SerializeField] private float missFlightDistance = 400f;

        private Gunner gunner;

        private void Awake()
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
            Vector3 shotEnd = hit.IsMiss ? hit.Ray.GetPoint(missFlightDistance) : hit.Nearest.point;

            int flocksCount = 0;
            foreach (Flocks flocks in Registers.Instance.GetAll<Flocks>(RegisterInactivePolicy.Exclude))
            {
                flocks.ScareRpc(hit.Ray.origin, shotEnd, scareRadius);
                flocksCount++;
            }

            Log.Info($"Entity {name} sent the {Vector3.Distance(hit.Ray.origin, shotEnd):0} m line of its shot to {flocksCount} bird flocks");
        }
    }
}
