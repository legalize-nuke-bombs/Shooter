using System;
using Shooter.Infrastructure;
using Unity.Behavior;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class GoToTool : LlmTool<GoToArguments>
    {
        private static readonly Journal Log = Logs.Here();
        private const float GroundReach = 5f;

        private BehaviorGraphAgent btAgent;
        private AgentMovement movement;

        public override string Name => "go_to";

        public override string Description =>
            @"
Walk to a point of the world by starting a second-level behavior tree action.
Your character automatically finds the path to the point and travels any distance, no matter how far.
Your character automatically avoids dangerous areas (cold bodies of water, etc.)
x, y, z: whole meters, the world coordinates shown next to everything you see and next to yourself in look_at_yourself; y is height. To reach a thing, pass its coordinates.
sprint: true to run.
The result comes at once, the walk itself takes time: you will be notified when you arrive or when your character failed to find path.
To stop going, call `halt_bt` tool.
";

        protected override void Start()
        {
            btAgent = Self.GetComponent<BehaviorGraphAgent>();
            if (btAgent == null)
            {
                Log.Error($"Entity {Self.name} does not have BehaviourGraphAgent component required by tool {Name}");
            }
            movement = Self.GetComponent<AgentMovement>();
            if (movement == null)
            {
                Log.Error($"Entity {Self.name} does not have AgentMovement component required by tool {Name}");
            }
        }

        protected override string Execute(GoToArguments arguments, LlmCallContext context)
        {
            Vector3 target = new(arguments.X, arguments.Y, arguments.Z);
            string targetFormatted = Whereabouts.FormatCoordinates(target);

            if (!AgentMovement.TryFindNearestGround(target, GroundReach, out Vector3 groundPoint))
            {
                return $"There is no walkable ground at {targetFormatted}";
            }
            if (!movement.TryPlanPathEnd(groundPoint, out Vector3 pathEnd))
            {
                return $"There is no way from here to {targetFormatted}";
            }

            float shortfallDistance = Vector3.Distance(pathEnd, groundPoint);
            if (shortfallDistance > movement.ShortfallLimit)
            {
                return $"There is no way from here to {targetFormatted}: the nearest walkable ground is {shortfallDistance:F0} m short of it";
            }

            string shortfallDescription = Whereabouts.DescribeShortfall(groundPoint - pathEnd);
            if (shortfallDescription.Length > 0 && Vector3.Distance(pathEnd, Self.transform.position) < Whereabouts.ArrivalTolerance)
            {
                return $"There is no way from here toward {targetFormatted}: the walkable ground ends right here, {shortfallDescription}";
            }

            btAgent.SetVariableValue("CustomOrderCurrent", CustomOrder.GoTo);
            btAgent.SetVariableValue("CustomOrderGoToTarget", target);
            btAgent.SetVariableValue("CustomOrderGoToSprint", arguments.Sprint);

            return $"You are now going to {targetFormatted}. You will be notified when you will arrive.";
        }
    }
}
