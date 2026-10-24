using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Artifact", fileName = "Artifact")]
    public class ArtifactAsset : UniqueItemAsset
    {
        [SerializeField] private List<ArtifactEffect> effects;

        public List<ArtifactEffect> Effects => effects;

        public override UniqueItem CreateItem()
        {
            return new Artifact(Id);
        }
    }
}
