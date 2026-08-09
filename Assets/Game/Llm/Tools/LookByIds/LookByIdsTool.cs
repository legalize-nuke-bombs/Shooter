using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Identity;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    public class LookByIdsTool : LlmTool<LookByIdsArguments>
    {
        private Digester digester;
        private PersistentIds ids;

        protected override void Awake()
        {
            base.Awake();
            digester = GetComponent<Digester>();
            ids = GetComponent<PersistentIds>();
        }

        public override string Name => "look_by_ids";

        public override string Description =>
            "Look by IDs: character statuses by their IDs.";

        protected override string Execute(LookByIdsArguments arguments)
        {
            long[] targetIds = arguments.TargetIds;

            var sb = new StringBuilder();

            foreach (long targetId in targetIds)
            {
                GameObject target = ids.Of(targetId).gameObject;
                if (target == null)
                {
                    sb.AppendLine($"Character with ID {targetId} does not exist");
                }
                else
                {
                    sb.AppendLine(digester.Of(target, DigestionDetail.Brief));
                }
            }

            return sb.ToString();
        }
    }
}
