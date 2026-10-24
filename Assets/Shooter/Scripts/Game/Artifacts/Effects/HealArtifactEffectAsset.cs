using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Heal Artifact Effect", fileName = "HealArtifactEffect")]
    public class HealArtifactEffectAsset : ArtifactEffectAsset
    {
        [SerializeField] private float speed;

        public override ArtifactEffect Create()
        {
            return new HealArtifactEffect(speed);
        }
    }
}
