using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(WorldDigester))]
    public sealed class LookAroundTool : LlmTool<LookAroundArguments>
    {
        private WorldDigester worldDigester;

        protected override void Awake()
        {
            base.Awake();
            worldDigester = GetComponent<WorldDigester>();
        }

        public override string Name => "look_around";

        public override string Description => "This tool shows everything near you. The greater the distance, the less detail is visible.";

        protected override string Execute(LookAroundArguments arguments)
        {
            return "Objects around you:\n" + worldDigester.Digest();
        }
    }
}
