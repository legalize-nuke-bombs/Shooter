using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gunner))]
    [RequireComponent(typeof(Character))]
    public class DealDamageOnShot : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Gunner gunner;
        private Character character;

        public event Action<FirearmAsset, Health, BodyPart, int> DamageDealt;

        private void Awake()
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
            Health victim = hit.IsMiss ? null : hit.Nearest.collider.GetComponentInParent<Health>();
            if (victim == null)
            {
                Log.Info($"Entity {name} dealt no damage: the shot hit no one");
                return;
            }

            BodyPart part = FindWeakestHitBodyPart(hit.Hits, victim);
            int damage = Mathf.RoundToInt(asset.Damage * part.GetDamageMultiplier());
            victim.TakeDamage(damage, character, asset.DamageType);
            Log.Info($"Entity {name} dealt {damage} damage to {victim.name} in {part}");
            DamageDealt?.Invoke(asset, victim, part, damage);
        }

        private static BodyPart FindWeakestHitBodyPart(IReadOnlyList<RaycastHit> hits, Health victim)
        {
            BodyPart weakestPart = BodyPart.Torso;
            float highestMultiplier = 0f;

            for (int i = 0; i < hits.Count; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider.GetComponentInParent<Health>() != victim) continue;

                Hitbox hitbox = hitCollider.GetComponent<Hitbox>();
                BodyPart part = hitbox == null ? BodyPart.Torso : hitbox.Part;
                if (part.GetDamageMultiplier() <= highestMultiplier) continue;

                weakestPart = part;
                highestMultiplier = part.GetDamageMultiplier();
            }

            return weakestPart;
        }
    }
}
