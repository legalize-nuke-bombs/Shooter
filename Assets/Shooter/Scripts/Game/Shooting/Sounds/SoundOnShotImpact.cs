using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnShotImpact : MonoBehaviour
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
            if (hit.IsMiss)
            {
                Log.Info($"Entity {name} played no impact sound: the shot missed");
                return;
            }

            PhysicsMaterial surface = Surface.FindAt(hit.Nearest);
            string surfaceName = surface == null ? "no surface" : surface.name;
            SoundAsset sound = asset.ImpactSounds == null ? null : asset.ImpactSounds.GetSoundOn(surface);
            if (sound == null)
            {
                Log.Info($"Entity {name} played no impact sound: {asset.name} has none on {surfaceName}");
                return;
            }

            speaker.PlayAt(sound, hit.Nearest.point);
            Log.Info($"Entity {name} played the impact sound {sound.name} on {surfaceName} at {hit.Nearest.point}");
        }
    }
}
