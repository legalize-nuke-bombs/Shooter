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
            "Look by IDs: character statuses by their IDs.";

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
                Character target = Character.FindById(targetId, RegisterInactivePolicy.Include);

                if (target == null)
                {
                    result.AppendLine($"Character with ID {targetId} does not exist");
                }
                else
                {
                    result.AppendLine(Digester.DigestEntity(target.gameObject, DigestionDetail.Brief));
                }
            }

            return result.ToString();
        }
    }
}
