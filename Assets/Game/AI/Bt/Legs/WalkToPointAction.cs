using System;
using Shooter.Game.Body;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.Legs
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Walk To Point",
        description: "Asks the legs for the point every tick it runs; the body walks only while asked. Fails while the body reports no way to the point.",
        story: "[Agent] walks to [Destination], running if [Sprint]",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a08")]
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

            movement.Walk(Destination.Value, Sprint.Value);
            return movement.Status == AgentMovementStatus.Unreachable ? Status.Failure : Status.Success;
        }
    }
}
