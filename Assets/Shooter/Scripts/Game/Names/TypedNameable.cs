using UnityEngine;

namespace Shooter.Game
{
    public sealed class TypedNameable : Nameable
    {
        [SerializeField] private NameAsset asset;

        public NameAsset Asset => asset;

        public void AssignNameAsset(NameAsset nameAsset)
        {
            asset = nameAsset;
        }

        public override string Digest(DigestionDetail detail)
        {
            return asset == null ? null : asset.GetPromptName();
        }

        public override string GetPromptName()
        {
            return asset == null ? null : asset.GetPromptName();
        }
    }
}
