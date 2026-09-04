using System;
using Shooter.Game.Core;
using Shooter.Logging;

namespace Shooter.Game.Llm.LookAroundEntity
{
    [Serializable]
    public class LookAroundEntityTool : LlmTool<LookAroundEntityArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "look_around_entity";

        public override string Description =>
            "This tool shows everything around entity whose ID was passed. The greater the distance, the less detail is visible. You can use this tool to observe other characters.";

        public override void OnStart(LlmInitContext context)
        {}

        protected override string Execute(LookAroundEntityArguments arguments, LlmCallContext context)
        {
            long targetId = arguments.TargetId;

            var id = Character.Of(targetId, Inactive.Exclude);
            if (id == null)
            {
                Log.Info($"Entity {context.Self.name} tried to look around unknown entity");
                return $"Failed to find entity with ID {targetId}";
            }

            return $"Objects around entity {targetId}:\n" + WorldDigester.Current.Digest(id.transform.position);
        }
    }
}
