using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnHeadshot : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private EarSpeaker earSpeaker;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        protected override void ShooterOnEnable()
        {
            gunner.Shot += PlayOnHeadshot;
        }

        protected override void ShooterOnDisable()
        {
            gunner.Shot -= PlayOnHeadshot;
        }

        private void PlayOnHeadshot(FirearmAsset asset, ShotHit hit)
        {
            if (hit.Victim == null || hit.Part != BodyPart.Head)
            {
                Log.Info($"Entity {name} played no headshot sound: {(hit.Victim == null ? "the shot hit no one" : $"it hit {hit.Victim.name} in {hit.Part}")}");
                return;
            }

            earSpeaker.PlayForOwner(asset.HeadshotSound);
            Log.Info($"Entity {name} played the headshot sound of {asset.name} to its owner for {hit.Victim.name}");
        }
    }
}
