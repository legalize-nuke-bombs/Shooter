using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(WorldDigester))]
    public sealed class LookAroundTool : LlmTool<LookAroundArguments>
    {
        private WorldDigester worldDigester;

        public override string Name => "look_around";

        public override string Description =>
            "This tool shows everything near you. The greater the distance, the less detail is visible.";

        protected override void Awake()
        {
            base.Awake();
            worldDigester = GetComponent<WorldDigester>();
        }

        protected override string Execute(LookAroundArguments arguments, LlmCallContext context)
        {
            return "Objects around you:\n" + worldDigester.Digest();
        }
    }
}
