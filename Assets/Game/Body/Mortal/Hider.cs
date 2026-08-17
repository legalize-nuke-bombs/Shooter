using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Skin))]
    public class Hider : MonoBehaviour
    {
        private GameObject flesh;
        private Health health;
        private bool hidden;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            flesh = GetComponent<Skin>().Flesh;
        }

        private void Update()
        {
            if (flesh == null) return;

            bool dead = !health.Alive;
            if (dead == hidden) return;

            hidden = dead;
            flesh.SetActive(!dead);
        }
    }
}
