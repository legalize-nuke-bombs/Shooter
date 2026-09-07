using System;
using Shooter.Game.AI.Bt.CustomOrders;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.Follow
{
    [Serializable]
    public sealed class FollowTool : LlmTool<FollowArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private BtCustomOrderQueue customOrders;
        private AgentMovement movement;

        public override string Name => "follow";

        public override string Description =>
            @"
Follow a character by starting a second-level behavior tree action.
Your character walks after the target wherever it goes, finds the path itself and keeps following until the target is gone or you stop.
target_id: the ID of the character to follow, shown next to it in what you see.
distance: whole meters to keep from the target; your character stops when it is that close and walks again when the target moves away.
sprint: true to run.
force: by default the call is refused while another second-level action is active; set force to true to drop it and start this one at once.
The result comes at once, the following itself goes on: you will be notified when the target is gone. Use look_at_yourself to check the active second-level action and halt_bt to stop following.
";

        protected override void OnStart()
        {
            customOrders = Self.GetComponent<BtCustomOrderQueue>();
            if (customOrders == null)
            {
                Log.Error($"Entity {Self.name} does not have BtCustomOrderQueue component required by tool {Name}");
            }
            movement = Self.GetComponent<AgentMovement>();
            if (movement == null)
            {
                Log.Error($"Entity {Self.name} does not have AgentMovement component required by tool {Name}");
            }
        }

        protected override string Execute(FollowArguments arguments, LlmCallContext context)
        {
            Character target = Character.Of(arguments.TargetId, Inactive.Exclude);
            if (target == null) return $"Character with ID {arguments.TargetId} does not exist";
            if (target.gameObject == Self) return "You cannot follow yourself";
            if (arguments.Distance < 0) return "distance cannot be negative";

            var order = new BtCoFollow { TargetId = arguments.TargetId, Reach = arguments.Distance, Sprint = arguments.Sprint };
            Vector3 there = target.transform.position;

            if (!movement.TryPlan(there, out Vector3 end))
            {
                return $"There is no way from here to {order.Handle}";
            }

            float rest = Vector3.Distance(end, there);
            if (rest > movement.ShortfallLimit)
            {
                return $"There is no way from here to {order.Handle}: the nearest walkable ground is {rest:F0} m short of it";
            }

            string started = order.PromptDescription(Self);

            if (arguments.Force)
            {
                BtCustomOrder dropped = customOrders.Current;
                customOrders.ForcePut(order);
                return dropped == null
                    ? $"Started: {started}"
                    : $"Dropped: {dropped.PromptDescription(Self)}\nStarted: {started}";
            }

            if (customOrders.TryPut(order)) return $"Started: {started}";

            return @$"Refused, another second-level action is active: {customOrders.Current.PromptDescription(Self)}
Call again with force=true to replace it, or halt_bt to stop it";
        }
    }
}
