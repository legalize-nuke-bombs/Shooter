using System;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public class TakeDamageOnSurface : MonoBehaviour, ITimePassedHandler
    {
        private Health health;
        private CharacterController characterController;

        [SerializeField] private PhysicsMaterial surfaceMaterial;
        [SerializeField] private float damagePerGameSecond = 0.5f;
        [SerializeField] private DamageAsset damageType;

        [SerializeField] private float hitsRealSecondInterval = 0.75f;
        private float hitsRealSecondSum = 0;
        private float gameSecondsSum = 0;

        private void Awake()
        {
            health = GetComponent<Health>();
            characterController = GetComponent<CharacterController>();
        }

        public void HandleTimePassed(float seconds)
        {
            if (!health.IsAlive)
            {
                return;
            }
            hitsRealSecondSum += Time.deltaTime;
            gameSecondsSum += seconds;
            if (hitsRealSecondSum < hitsRealSecondInterval)
            {
                return;
            }
            if (Surface.FindUnder(characterController) == surfaceMaterial)
            {
                health.TakeDamage(damagePerGameSecond * gameSecondsSum, null, damageType);
            }
            hitsRealSecondSum = 0;
            gameSecondsSum = 0;
        }
    }
}


