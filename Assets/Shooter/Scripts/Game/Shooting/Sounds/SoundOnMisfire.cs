using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnMisfire : MonoBehaviour
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
            gunner.Misfired += Play;
        }

        private void OnDisable()
        {
            gunner.Misfired -= Play;
        }

        private void Play(FirearmAsset asset)
        {
            speaker.Play(asset.MisfireSound);
            Log.Info($"Entity {name} played the misfire sound of {asset.name}");
        }
    }
}
