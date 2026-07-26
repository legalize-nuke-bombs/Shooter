using System.Threading.Tasks;

namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        private const string Refusal = "Not now.";

        protected override Task<string> Answer(Conversation conversation)
        {
            return Task.FromResult(Refusal);
        }
    }
}
