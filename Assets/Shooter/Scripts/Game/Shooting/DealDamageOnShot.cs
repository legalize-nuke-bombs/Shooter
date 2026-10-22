using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Character))]
    public class DealDamageOnShot : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private Character character;

        protected override void ShooterAwake()
        {
            gunner = GetComponent<Gunner>();
            character = GetComponent<Character>();
        }

        private void OnEnable()
        {
            gunner.Shot += DealDamage;
        }

        private void OnDisable()
        {
            gunner.Shot -= DealDamage;
        }

        private void DealDamage(FirearmAsset asset, ShotHit hit)
        {
            if (hit.Victim == null)
            {
                Log.Info($"Entity {name} dealt no damage: the shot hit no one");
                return;
            }

            int damage = Mathf.RoundToInt(asset.Damage * hit.Part.GetDamageMultiplier());
            hit.Victim.TakeDamage(damage, character, asset.DamageType);
            Log.Info($"Entity {name} dealt {damage} damage to {hit.Victim.name} in {hit.Part}");
        }
    }
}
