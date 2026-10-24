using UnityEngine;

namespace Shooter.Game
{
    public class HealArtifactEffect : ArtifactEffect
    {
        private readonly float healPerHour;

        private Health health;

        public HealArtifactEffect(float healPerHour)
        {
            this.healPerHour = healPerHour;
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
            float amount = healPerHour * dt / Clock.HourLengthSeconds;
            health.Heal(amount);
        }
    }
}
