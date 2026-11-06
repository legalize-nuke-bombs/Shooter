using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Walk To Point",
        story: "[Agent] walks to [Destination], running if [Sprint]",
        category: "Action",
        id: "5ccd2c8fcf5d4d06bbdec0d4a6f0033f")]
    public partial class WalkToPointAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Vector3> Destination = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private AgentMovement movement;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (movement == null)
            {
                movement = Agent.Value.GetComponent<AgentMovement>();
                if (movement == null) return Status.Failure;
            }

            movement.WalkTo(Destination.Value, Sprint.Value);
            return movement.Status == AgentMovementStatus.Unreachable ? Status.Failure : Status.Success;
        }
    }
}
