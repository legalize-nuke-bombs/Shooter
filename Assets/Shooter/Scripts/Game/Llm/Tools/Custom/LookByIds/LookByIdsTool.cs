using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class LookByIdsTool : LlmTool<LookByIdsArguments>
    {
        public override string Name => "look_by_ids";

        public override string Description =>
            @"
Look by IDs: entities statuses by their IDs.
If the `detailed` field is passed as `true`, the response will contain the most comprehensive information about each entity (for example, this is useful when examining chests, corpses, etc.). If the `detailed` field is passed as `false`, the response will contain a concise summary (it is preferable to use this option whenever possible).
";

        protected override void HandleAttached()
        {
        }

        protected override string Execute(LookByIdsArguments arguments, LlmCallContext context)
        {
            long[] targetIds = arguments.TargetIds;
            if (targetIds == null || targetIds.Length == 0) return "Nothing to look at";

            StringBuilder result = new();

            foreach (long targetId in targetIds)
            {
                IncrementalId target = IncrementalId.FindByValue(targetId, RegisterInactivePolicy.Include);

                if (target == null)
                {
                    result.AppendLine($"Entity with ID {targetId} does not exist");
                }
                else
                {
                    result.AppendLine(Digester.DigestEntity(target.gameObject, arguments.Detailed ? DigestionDetail.Full : DigestionDetail.Brief));
                }
            }

            return result.ToString();
        }
    }
}
