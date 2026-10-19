using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class NameAsset : Asset
    {
        public abstract string GetShownName();
        public abstract string GetPromptName();
    }
}
