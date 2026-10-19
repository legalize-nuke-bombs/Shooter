using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class FollowTool : LlmTool<FollowArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private CustomOrderSlot customOrderSlot;
        private AgentMovement movement;

        public override string Name => "follow";

        public override string Description =>
            @"
Follow a character by starting a second-level behavior tree action.
Your character walks after the target wherever it goes, finds the path itself and keeps following until the target is gone, there is no way toward it, or you stop.
target_id: the ID of the character to follow, shown next to it in what you see.
distance: whole meters to keep from the target; your character stops when it is that close and walks again when the target moves away.
sprint: true to run.
force: by default the call is refused while another second-level action is active; set force to true to drop it and start this one at once.
The result comes at once, the following itself goes on: you will be notified when the target is gone or when there is no way toward it. Use look_at_yourself to check the active second-level action and halt_bt to stop following.
";

        protected override void HandleAttached()
        {
            customOrderSlot = Self.GetComponent<CustomOrderSlot>();
            if (customOrderSlot == null)
            {
                Log.Error($"Entity {Self.name} does not have CustomOrderSlot component required by tool {Name}");
            }
            movement = Self.GetComponent<AgentMovement>();
            if (movement == null)
            {
                Log.Error($"Entity {Self.name} does not have AgentMovement component required by tool {Name}");
            }
        }

        protected override string Execute(FollowArguments arguments, LlmCallContext context)
        {
            Character target = Character.FindById(arguments.TargetId, RegisterInactivePolicy.Exclude);
            if (target == null) return $"Character with ID {arguments.TargetId} does not exist";
            if (target.gameObject == Self) return "You cannot follow yourself";
            if (arguments.Distance < 0) return "distance cannot be negative";

            FollowOrder order = new() { TargetId = arguments.TargetId, Reach = arguments.Distance, Sprint = arguments.Sprint };
            Vector3 targetPosition = target.transform.position;

            if (!movement.TryPlanPathEnd(targetPosition, out Vector3 pathEnd))
            {
                return $"There is no way from here to {order.TargetPromptName}";
            }

            float shortfallDistance = Vector3.Distance(pathEnd, targetPosition);
            if (shortfallDistance > movement.ShortfallLimit)
            {
                return $"There is no way from here to {order.TargetPromptName}: the nearest walkable ground is {shortfallDistance:F0} m short of it";
            }

            string startedOrderDescription = order.GetPromptDescription(Self);

            if (arguments.Force)
            {
                CustomOrder droppedOrder = customOrderSlot.CurrentOrder;
                customOrderSlot.ForcePutOrder(order);
                return droppedOrder == null
                    ? $"Started: {startedOrderDescription}"
                    : $"Dropped: {droppedOrder.GetPromptDescription(Self)}\nStarted: {startedOrderDescription}";
            }

            if (customOrderSlot.TryPutOrder(order)) return $"Started: {startedOrderDescription}";

            return @$"Refused, another second-level action is active: {customOrderSlot.CurrentOrder.GetPromptDescription(Self)}
Call again with force=true to replace it, or halt_bt to stop it";
        }
    }
}
