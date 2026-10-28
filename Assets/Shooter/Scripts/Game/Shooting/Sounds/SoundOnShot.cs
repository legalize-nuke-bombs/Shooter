using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnShot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private Speaker speaker;

        private void Awake()
        {
            gunner = GetComponent<Gunner>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            gunner.Shot += Play;
        }

        private void OnDisable()
        {
            gunner.Shot -= Play;
        }

        private void Play(FirearmAsset asset, ShotHit hit)
        {
            speaker.Play(asset.ShotSound);
            Log.Info($"Entity {name} played the shot sound of {asset.name}");
        }
    }
}
