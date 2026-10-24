using UnityEngine;

namespace Shooter.Game
{
    public class HealArtifactEffect : ArtifactEffect
    {
        private readonly float speed;

        private Health health;

        public HealArtifactEffect(float speed)
        {
            this.speed = speed;
        }

        public override void Enable(GameObject self)
        {
            health = self.GetComponent<Health>();
        }

        public override void Disable()
        {

        }

        public override void Update(float dt)
        {
            if (health == null)
            {
                return;
            }
            float amount = speed * dt;
            health.Heal(amount);
        }
    }
}
