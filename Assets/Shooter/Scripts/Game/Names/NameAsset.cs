using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class NameAsset : Asset
    {
        public abstract string Text();
        public abstract string Prompt();
    }
}
