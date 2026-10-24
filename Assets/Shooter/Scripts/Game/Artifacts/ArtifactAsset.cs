using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Artifact", fileName = "Artifact")]
    public class ArtifactAsset : UniqueItemAsset
    {
        [SerializeField] private List<ArtifactEffectAsset> effects;

        public List<ArtifactEffectAsset> Effects => effects;

        public override ItemStats Stats => effects.Aggregate(new ItemStats(), (sum, effect) => sum + effect.Stats);

        public override UniqueItem CreateItem()
        {
            List<ArtifactEffect> clonedEffects = new();
            foreach (ArtifactEffectAsset effectAsset in effects)
            {
                clonedEffects.Add(effectAsset.Create());
            }
            return new Artifact(Id, clonedEffects);
        }
    }
}
