using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DealDamageOnShot))]
    [RequireComponent(typeof(EarSpeaker))]
    public class SoundOnHeadshot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private DealDamageOnShot dealDamageOnShot;
        private EarSpeaker earSpeaker;

        private void Awake()
        {
            dealDamageOnShot = GetComponent<DealDamageOnShot>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        private void OnEnable()
        {
            dealDamageOnShot.DamageDealt += PlayOnHeadshot;
        }

        private void OnDisable()
        {
            dealDamageOnShot.DamageDealt -= PlayOnHeadshot;
        }

        private void PlayOnHeadshot(FirearmAsset asset, Health victim, BodyPart part, int damage)
        {
            if (part != BodyPart.Head)
            {
                Log.Info($"Entity {name} played no headshot sound: it hit {victim.name} in {part}");
                return;
            }

            earSpeaker.PlayForOwner(asset.HeadshotSound);
            Log.Info($"Entity {name} played the headshot sound of {asset.name} to its owner for {victim.name}");
        }
    }
}
