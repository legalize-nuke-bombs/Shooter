using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Skin))]
    public class SkinHiderWhileDead : MonoBehaviour
    {
        private GameObject modelInstance;
        private Health health;
        private bool hidden;

        private void Awake()
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
