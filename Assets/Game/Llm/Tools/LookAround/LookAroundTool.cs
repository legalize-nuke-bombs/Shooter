using System;
using UnityEngine;

namespace Shooter.Game.Llm.LookAround
{
    [Serializable]
    public sealed class LookAroundTool : LlmTool<LookAroundArguments>
    {
        public override string Name => "look_around";

        public override string Description =>
            "This tool shows everything near you. The greater the distance, the less detail is visible.";

        private Transform transform;

        public override void OnStart(LlmInitContext context)
        {
            transform = context.Self.transform;
        }

        protected override string Execute(LookAroundArguments arguments, LlmCallContext context)
        {
            return "Objects around you:\n" + WorldDigester.Current.Digest(transform.position);
        }
    }
}
