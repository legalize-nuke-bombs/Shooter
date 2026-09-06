using System;
using Shooter.Game.AI.Bt.CustomOrders;
using Shooter.Game.Body;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

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
Walk to a point of the world by starting a second-level behavior tree action.
Your character automatically finds the path to the point and travels any distance, no matter how far.
task_name: arbitrary name of the walk, it comes back in the reports.
x, y, z: whole meters, the world coordinates shown next to everything you see and next to yourself in look_at_yourself; y is height. To reach a thing, pass its coordinates.
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
            var target = new Vector3(arguments.X, arguments.Y, arguments.Z);
            string point = Whereabouts.Coordinates(target);

            if (!AgentMovement.NearestGround(target, GroundReach, out Vector3 ground))
            {
                return $"There is no walkable ground at {point}";
            }
            if (!movement.TryPlan(ground, out Vector3 end))
            {
                return $"There is no way from here to {point}";
            }

            float rest = Vector3.Distance(end, ground);
            if (rest > movement.ShortfallLimit)
            {
                return $"There is no way from here to {point}: the nearest walkable ground is {rest:F0} m short of it";
            }

            string shortfall = Whereabouts.Shortfall(ground - end);
            if (shortfall.Length > 0 && Vector3.Distance(end, Self.transform.position) < Whereabouts.ArrivalTolerance)
            {
                return $"There is no way from here toward {point}: the walkable ground ends right here, {shortfall}";
            }

            var order = new BtCoGoTo { Name = arguments.TaskName, Destination = ground, Sprint = arguments.Sprint };
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
