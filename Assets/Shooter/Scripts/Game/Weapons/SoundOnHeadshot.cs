using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnHeadshot : ShooterBehaviour
    {
        private Gunner gunner;
        private EarSpeaker earSpeaker;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        private void OnEnable()
        {
            gunner.Shot += PlayOnHeadshot;
        }

        private void OnDisable()
        {
            gunner.Shot -= PlayOnHeadshot;
        }

        private void PlayOnHeadshot(FirearmAsset asset, ShotHit hit)
        {
            if (hit.Victim == null || hit.Part != BodyPart.Head) return;

            earSpeaker.PlayForOwner(asset.HeadshotSound);
        }
    }
}
