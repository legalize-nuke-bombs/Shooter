using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Heal Artifact Effect", fileName = "HealArtifactEffect")]
    public class HealArtifactEffectAsset : ArtifactEffectAsset
    {
        [SerializeField] private float healPerHour;

        public override ArtifactEffect Create()
        {
            return new HealArtifactEffect(healPerHour);
        }

        public override ItemStats Stats => new() { HealPerHour = (int)healPerHour };
    }
}
