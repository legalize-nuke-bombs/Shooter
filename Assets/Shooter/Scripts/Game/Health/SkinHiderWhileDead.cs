using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Skin))]
    public class SkinHiderWhileDead : ShooterBehaviour
    {
        private GameObject modelInstance;
        private Health health;
        private bool hidden;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            modelInstance = GetComponent<Skin>().ModelInstance;
        }

        private void Update()
        {
            if (modelInstance == null) return;

            bool dead = !health.IsAlive;
            if (dead == hidden) return;

            hidden = dead;
            modelInstance.SetActive(!dead);
        }
    }
}
