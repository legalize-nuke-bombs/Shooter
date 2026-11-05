using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Walk Toward Target",
        description: "Asks the legs, every tick it runs, for the point the reach short of the target; stands while the target is within the reach; aims afresh once the target has drifted two metres from where it was aimed at or the body has been moved by someone else. Fails while the body reports no way toward the target.",
        story: "[Agent] walks toward [Target] keeping [Reach], running if [Sprint]",
        category: "Action",
        id: "4f7a0c2e9b3d4a61b8e5d2c7f0a13b22")]
    public partial class WalkTowardTargetAction : Action
    {
        private const float MaxTargetDrift = 2f;

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Transform> Target = new();
        [SerializeReference] public BlackboardVariable<float> Reach = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private AgentMovement movement;
        private bool hasAimPoint;
        private Vector3 targetPositionAtAim;
        private Vector3 aimPoint;

        protected override Status OnStart()
        {
            if (Agent.Value == null || Target.Value == null) return Status.Failure;

            if (movement == null)
            {
                movement = Agent.Value.GetComponent<AgentMovement>();
                if (movement == null) return Status.Failure;
            }

            Vector3 targetPosition = Target.Value.position;
            Vector3 feetPosition = movement.FeetPosition;
            Vector3 offsetFromTarget = new(feetPosition.x - targetPosition.x, 0f, feetPosition.z - targetPosition.z);

            if (offsetFromTarget.magnitude <= Reach.Value)
            {
                hasAimPoint = false;
                return Status.Success;
            }

            if (movement.Status == AgentMovementStatus.Displaced)
            {
                hasAimPoint = false;
                return Status.Success;
            }

            if (!hasAimPoint || Vector3.Distance(targetPosition, targetPositionAtAim) > MaxTargetDrift)
            {
                targetPositionAtAim = targetPosition;
                aimPoint = targetPosition + offsetFromTarget.normalized * Reach.Value;
                hasAimPoint = true;
            }

            movement.WalkTo(aimPoint, Sprint.Value);
            return movement.Status == AgentMovementStatus.Unreachable ? Status.Failure : Status.Success;
        }
    }
}
