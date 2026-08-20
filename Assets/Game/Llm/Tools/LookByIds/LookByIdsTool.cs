using System.Text;
using Shooter.Game.Core;

namespace Shooter.Game.Llm
{
    public class LookByIdsTool : LlmTool<LookByIdsArguments>
    {
        private Digester digester;

        public override string Name => "look_by_ids";

        public override string Description =>
            "Look by IDs: character statuses by their IDs.";

        protected override void Awake()
        {
            base.Awake();
            digester = GetComponent<Digester>();
        }

        protected override string Execute(LookByIdsArguments arguments)
        {
            long[] targetIds = arguments.TargetIds;
            if (targetIds == null || targetIds.Length == 0) return "Nothing to look at";

            var sb = new StringBuilder();

            foreach (long targetId in targetIds)
            {
                Character target = Character.Of(targetId);

                if (target == null)
                    sb.AppendLine($"Character with ID {targetId} does not exist");
                else
                    sb.AppendLine(digester.Of(target.gameObject, DigestionDetail.Brief));
            }

            return sb.ToString();
        }
    }
}
