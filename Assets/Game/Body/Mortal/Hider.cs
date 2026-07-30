using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Health))]
    public class Hider : MonoBehaviour
    {
        private Health health;
        private GameObject flesh;
        private bool hidden;

        private void Awake()
        {
            health = GetComponent<Health>();

            var animator = GetComponentInChildren<Animator>();
            flesh = animator == null ? null : animator.gameObject;
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
