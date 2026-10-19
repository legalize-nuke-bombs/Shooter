using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public sealed class TypedNameable : Nameable
    {
        [SerializeField] private NameAsset asset;

        public NameAsset Asset => asset;

        public void Assign(NameAsset assigned)
        {
            asset = assigned;
        }

        public override string Digest(DigestionDetail detail)
        {
            return asset == null ? null : asset.Prompt();
        }

        public override string PromptName()
        {
            return asset == null ? null : asset.Prompt();
        }
    }
}
