using System;
using Shooter.Infrastructure;
using UnityEngine;
using Unity.Behavior;

namespace Shooter.Game
{
    [Serializable]
    public sealed class FollowTool : LlmTool<FollowArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private BehaviorGraphAgent btAgent;
        private AgentMovement movement;

        public override string Name => "follow";

        public override string Description =>
            @"
Follow a character by starting a second-level behavior tree action.
Your character walks after the target wherever it goes, finds the path itself and keeps following until the target is gone, there is no way toward it, or you stop.
Your character automatically avoids dangerous areas (cold bodies of water, etc.)
target_id: the ID of the character to follow, shown next to it in what you see.
reach: whole meters to keep from the target; your character stops when it is that close and walks again when the target moves away.
sprint: true to run.
The result comes at once, the following itself goes on: you will be notified when the target is gone or when there is no way toward it.
If the target is reachable and still exists, your character will never stop following it automatically. You must always call halt_bt tool manually to stop following.
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

        protected override string Execute(FollowArguments arguments, LlmCallContext context)
        {
            Character targetCharacter = Character.FindById(arguments.TargetId, RegisterInactivePolicy.Include);
            if (targetCharacter == null) return $"Character with ID {arguments.TargetId} does not exist";
            ShooterEntity.ComponentStatus status = ShooterEntity.GetComponentStatus(targetCharacter);
            if (status == ShooterEntity.ComponentStatus.Optimized) return $"Character with ID {arguments.TargetId} is too far";
            if (status != ShooterEntity.ComponentStatus.Enabled) return $"Character with ID {arguments.TargetId} does not exist";
            if (targetCharacter.gameObject == Self) return "You cannot follow yourself";
            if (arguments.Reach < 0) return "Distance cannot be negative";

            Vector3 targetPosition = targetCharacter.transform.position;

            if (!movement.TryPlanPathEnd(targetPosition, out Vector3 pathEnd))
            {
                return $"There is no way from here to character ID {arguments.TargetId}";
            }

            float shortfallDistance = Vector3.Distance(pathEnd, targetPosition);
            if (shortfallDistance > movement.ShortfallLimit)
            {
                return $"There is no way from here to character ID {arguments.TargetId}: the nearest walkable ground is {shortfallDistance:F0} m short of it";
            }

            btAgent.SetVariableValue("CustomOrderCurrent", CustomOrder.Follow);
            btAgent.SetVariableValue("CustomOrderFollowTarget", targetCharacter.gameObject);
            btAgent.SetVariableValue("CustomOrderFollowReach", arguments.Reach);
            btAgent.SetVariableValue("CustomOrderFollowSprint", arguments.Sprint);

            return $"You are now following character ID {arguments.TargetId}. Remember to use the `halt_bt` tool as soon as you need to stop following them.";
        }
    }
}
