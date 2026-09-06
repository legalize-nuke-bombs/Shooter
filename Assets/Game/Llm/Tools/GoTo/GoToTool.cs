using System;
using Shooter.Game.AI.Bt.CustomOrders;
using Shooter.Game.Body;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Llm.GoTo
{
    [Serializable]
    public sealed class GoToTool : LlmTool<GoToArguments>
    {
        private static readonly Journal Log = Logs.Here();
        private const float GroundReach = 5f;

        private BtCustomOrderQueue customOrders;
        private AgentMovement movement;

        public override string Name => "go_to";

        public override string Description =>
            @"
Walk in a direction for a distance by starting a second-level behavior tree action.
Your character automatically finds the path to the target and travels any distance, no matter how far.
taskName: arbitrary name of the action
bearing: degrees clockwise from north, 0 north, 90 east, 180 south, 270 west; the number in parentheses next to everything you see.
distance: whole meters.
height: whole meters the point lies above your own feet, negative below, 0 for your own level; things on another level show it next to them as ""3 m up"" or ""3 m down"", pass that number, otherwise leave 0.
sprint: true to run.
force: by default the call is refused while another second-level action is active; set force to true to drop it and start this one at once.
The result comes at once, the walk itself takes time: you will be notified when you arrive or when your character failed to find path. Use look_at_yourself to check the active second-level action.
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

        protected override string Execute(GoToArguments arguments, LlmCallContext context)
        {
            if (arguments.Distance < 1)
            {
                return "Distance must be at least 1 meter";
            }

            int bearing = Cardinal.Bearing(arguments.Bearing);
            Vector3 target = Self.transform.position + Quaternion.Euler(0f, bearing, 0f) * Vector3.forward * arguments.Distance + Vector3.up * arguments.Height;

            if (!NavMesh.SamplePosition(target, out NavMeshHit ground, GroundReach, NavMesh.AllAreas))
            {
                return $"There is no walkable ground at {arguments.TaskName}";
            }
            if (!movement.TryPlan(ground.position, out Vector3 end))
            {
                return $"There is no way from here to {arguments.TaskName}";
            }

            string shortfall = Cardinal.Shortfall(ground.position - end);
            if (shortfall.Length > 0 && Vector3.Distance(end, Self.transform.position) < Cardinal.ArrivalTolerance)
            {
                return $"There is no way from here toward {arguments.TaskName}: the walkable ground ends right here, {shortfall}";
            }

            var order = new BtCoGoTo { Name = arguments.TaskName, Destination = ground.position, Sprint = arguments.Sprint };
            string started = order.PromptDescription(Self);
            if (shortfall.Length > 0) started += $"\nThe walkable ground ends {shortfall}";

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
