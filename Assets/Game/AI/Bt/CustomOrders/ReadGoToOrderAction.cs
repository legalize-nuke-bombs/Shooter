using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Read Go To Order",
        description: "Reads the go_to custom order from the slot: its destination and pace go into the variables; fails when the slot holds no go_to order.",
        story: "[Agent] reads the go_to order into [Destination]",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a09")]
    public partial class ReadGoToOrderAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Vector3> Destination = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private BtCustomOrderQueue customOrders;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (customOrders == null)
            {
                customOrders = Agent.Value.GetComponent<BtCustomOrderQueue>();
                if (customOrders == null) return Status.Failure;
            }

            if (customOrders.Current is not BtCoGoTo order) return Status.Failure;

            order.Begin();
            Destination.Value = order.Destination;
            Sprint.Value = order.Sprint;
            return Status.Success;
        }
    }
}
