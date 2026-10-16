using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class NameSpec : Spec
    {
        public abstract string Text();
        public abstract string Prompt();
    }
}
