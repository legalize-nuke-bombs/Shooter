using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Read Go To Order",
        description: "Reads the go_to custom order from the slot: its destination and pace go into the variables; fails when the slot holds no go_to order.",
        story: "[Agent] reads the go_to order into [Destination] and [Sprint]",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a09")]
    public partial class ReadGoToOrderAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Vector3> Destination = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private CustomOrderSlot customOrderSlot;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (customOrderSlot == null)
            {
                customOrderSlot = Agent.Value.GetComponent<CustomOrderSlot>();
                if (customOrderSlot == null) return Status.Failure;
            }

            if (customOrderSlot.CurrentOrder is not GoToOrder order) return Status.Failure;

            order.MarkRunning();
            Destination.Value = order.Destination;
            Sprint.Value = order.Sprint;
            return Status.Success;
        }
    }
}
