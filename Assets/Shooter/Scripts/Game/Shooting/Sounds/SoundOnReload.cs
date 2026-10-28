using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnReload : MonoBehaviour
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
            gunner.ReloadStarted += Play;
        }

        private void OnDisable()
        {
            gunner.ReloadStarted -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.ReloadSound);
            Log.Info($"Entity {name} played the reload sound of {asset.name}");
        }
    }
}
