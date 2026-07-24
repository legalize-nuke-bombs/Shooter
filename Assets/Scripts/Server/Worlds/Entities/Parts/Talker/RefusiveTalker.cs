using System.Threading.Tasks;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public sealed class RefusiveTalker : Talker
    {
        private const string Refusal = "Not now.";

        public RefusiveTalker(Entity self) : base(self)
        {
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            return Task.FromResult(Refusal);
        }
    }
}
