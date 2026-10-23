using System;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public class LookByIdsTool : LlmTool<LookByIdsArguments>
    {
        [SerializeField] private float detailedModeMaxDistance = 5;

        public override string Name => "look_by_ids";

        public override string Description =>
            @$"
Look by IDs: entities statuses by their IDs.
Always look at a wanderer who starts a conversation with you before you respond.
If the `detailed` field is passed as `true`, the response will contain the most comprehensive information about each entity (for example, this is useful when examining chests, corpses, etc.). If the `detailed` field is passed as `false`, the response will contain a concise summary (it is preferable to use this option whenever possible).
The detailed mode is available only for objects within a distance of {detailedModeMaxDistance} meters and closer.
";

        protected override void HandleAttached()
        {
        }

        protected override string Execute(LookByIdsArguments arguments, LlmCallContext context)
        {
            long[] targetIds = arguments.TargetIds;
            if (targetIds == null || targetIds.Length == 0) return "Nothing to look at";

            DigestionDetail mode = arguments.Detailed ? DigestionDetail.Full : DigestionDetail.Brief;

            StringBuilder result = new();

            foreach (long targetId in targetIds)
            {
                IncrementalId target = IncrementalId.FindByValue(targetId, RegisterInactivePolicy.Include);

                if (target == null)
                {
                    result.AppendLine($"Entity with ID {targetId} does not exist");
                    continue;
                }


                if (mode == DigestionDetail.Full)
                {
                    float distance = Vector3.Distance(Self.transform.position, target.transform.position);
                    if (distance > detailedModeMaxDistance)
                    {
                        result.AppendLine($"Entity ID {targetId} is too far for detailed mode ({distance} m), come closer or call this tool with the `detailed` set as `false`");
                        continue;
                    }
                }

                result.AppendLine(Digester.DigestEntity(target.gameObject, arguments.Detailed ? DigestionDetail.Full : DigestionDetail.Brief));
            }

            return result.ToString();
        }
    }
}
