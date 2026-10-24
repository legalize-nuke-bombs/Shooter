using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Heal Artifact Effect", fileName = "HealArtifactEffect")]
    public class HealArtifactEffect : ArtifactEffect
    {
        [SerializeField] private double speed;

        public override void Update(GameObject user, float dt)
        {
            Health health = user.GetComponent<Health>();
            if (health == null)
            {
                return;
            }
            double amount = speed * dt;
            health.Heal(amount);
        }
    }
}
