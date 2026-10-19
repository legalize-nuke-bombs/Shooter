using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class GoToTool : LlmTool<GoToArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();
        private const float GroundReach = 5f;

        private CustomOrderSlot customOrderSlot;
        private AgentMovement movement;

        public override string Name => "go_to";

        public override string Description =>
            @"
Walk to a point of the world by starting a second-level behavior tree action.
Your character automatically finds the path to the point and travels any distance, no matter how far.
task_name: arbitrary name of the walk, it comes back in the reports.
x, y, z: whole meters, the world coordinates shown next to everything you see and next to yourself in look_at_yourself; y is height. To reach a thing, pass its coordinates.
sprint: true to run.
force: by default the call is refused while another second-level action is active; set force to true to drop it and start this one at once.
The result comes at once, the walk itself takes time: you will be notified when you arrive or when your character failed to find path. Use look_at_yourself to check the active second-level action.
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

        protected override string Execute(GoToArguments arguments, LlmCallContext context)
        {
            Vector3 targetPoint = new(arguments.X, arguments.Y, arguments.Z);
            string targetCoordinates = Whereabouts.FormatCoordinates(targetPoint);

            if (!AgentMovement.TryFindNearestGround(targetPoint, GroundReach, out Vector3 groundPoint))
            {
                return $"There is no walkable ground at {targetCoordinates}";
            }
            if (!movement.TryPlanPathEnd(groundPoint, out Vector3 pathEnd))
            {
                return $"There is no way from here to {targetCoordinates}";
            }

            float shortfallDistance = Vector3.Distance(pathEnd, groundPoint);
            if (shortfallDistance > movement.ShortfallLimit)
            {
                return $"There is no way from here to {targetCoordinates}: the nearest walkable ground is {shortfallDistance:F0} m short of it";
            }

            string shortfallDescription = Whereabouts.DescribeShortfall(groundPoint - pathEnd);
            if (shortfallDescription.Length > 0 && Vector3.Distance(pathEnd, Self.transform.position) < Whereabouts.ArrivalTolerance)
            {
                return $"There is no way from here toward {targetCoordinates}: the walkable ground ends right here, {shortfallDescription}";
            }

            GoToOrder order = new() { TaskName = arguments.TaskName, Destination = groundPoint, Sprint = arguments.Sprint };
            string startedOrderDescription = order.GetPromptDescription(Self);
            if (shortfallDescription.Length > 0) startedOrderDescription += $"\nThe walkable ground ends {shortfallDescription}";

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
