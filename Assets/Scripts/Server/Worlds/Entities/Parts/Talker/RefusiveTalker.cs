using System.Threading.Tasks;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public sealed class RefusiveTalker : Talker
    {
        private const string Refusal = "Not now.";

        public RefusiveTalker(Entity self, Clock clock) : base(self, clock)
        {
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            return Task.FromResult(Refusal);
        }
    }
}
