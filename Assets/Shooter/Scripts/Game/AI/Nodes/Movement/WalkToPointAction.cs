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
        story: "[Agent] walks to [Destination], running if [Sprint], and stops at [StoppedAt]",
        category: "Action",
        id: "5ccd2c8fcf5d4d06bbdec0d4a6f0033f")]
    public partial class WalkToPointAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Vector3> Destination = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();
        [SerializeReference] public BlackboardVariable<Vector3> StoppedAt = new();

        private AgentMovement movement;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (movement == null)
            {
                movement = Agent.Value.GetComponent<AgentMovement>();
                if (movement == null) return Status.Failure;
            }

            return Walk();
        }

        protected override Status OnUpdate()
        {
            return Walk();
        }

        private Status Walk()
        {
            movement.WalkTo(Destination.Value, Sprint.Value);
            if (movement.Status == AgentMovementStatus.Walking) return Status.Running;

            StoppedAt.Value = movement.FeetPosition;
            return movement.Status == AgentMovementStatus.Arrived ? Status.Success : Status.Failure;
        }
    }
}
