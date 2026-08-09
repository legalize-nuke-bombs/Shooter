using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(Digester))]
    [RequireComponent(typeof(WorldDigester))]
    public sealed class LookAroundTool : LlmTool<LookAroundArguments>
    {
        private Digester digester;
        private WorldDigester worldDigester;

        private void Awake()
        {
            digester = GetComponent<Digester>();
            worldDigester = GetComponent<WorldDigester>();
        }

        public override string Name => "look_around";

        public override string Description =>
            "Look around: your own state and everything visible near you right now.";

        protected override string Execute(LookAroundArguments arguments)
        {
            return "Game time: " + Environment.Current.Clock.DateTime() + "\n" +
                   "Your state:\n" + digester.Of(gameObject, DigestionDetail.Full) + "\n" +
                   "Objects around you:\n" + worldDigester.Digest();
        }
    }
}
