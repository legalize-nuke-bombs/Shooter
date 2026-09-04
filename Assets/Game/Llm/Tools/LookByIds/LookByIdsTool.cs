using System;
using System.Text;
using Shooter.Game.Core;

namespace Shooter.Game.Llm.LookByIds
{
    [Serializable]
    public class LookByIdsTool : LlmTool<LookByIdsArguments>
    {
        public override string Name => "look_by_ids";

        public override string Description =>
            "Look by IDs: character statuses by their IDs.";

        protected override void OnStart()
        {
        }

        protected override string Execute(LookByIdsArguments arguments, LlmCallContext context)
        {
            long[] targetIds = arguments.TargetIds;
            if (targetIds == null || targetIds.Length == 0) return "Nothing to look at";

            var sb = new StringBuilder();

            foreach (long targetId in targetIds)
            {
                var target = Character.Of(targetId, Inactive.Exclude);

                if (target == null)
                {
                    sb.AppendLine($"Character with ID {targetId} does not exist");
                }
                else
                {
                    sb.AppendLine(Digester.Current.Of(target.gameObject, DigestionDetail.Brief));
                }
            }

            return sb.ToString();
        }
    }
}
