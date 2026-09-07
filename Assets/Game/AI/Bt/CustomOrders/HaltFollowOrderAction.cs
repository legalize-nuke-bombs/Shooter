using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Halt Follow Order",
        description: "Ends the follow custom order in the slot and hands its outcome to the mind: the target is gone, or there is no way toward it from where the body stands; fails when the slot holds no follow order.",
        story: "[Agent] halts the follow order",
        category: "Action",
        id: "4f7a0c2e9b3d4a61b8e5d2c7f0a13b23")]
    public partial class HaltFollowOrderAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue customOrders;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (customOrders == null)
            {
                customOrders = Agent.Value.GetComponent<BtCustomOrderQueue>();
                if (customOrders == null) return Status.Failure;
            }

            if (customOrders.Current is not BtCoFollow) return Status.Failure;

            customOrders.Finish();
            return Status.Success;
        }
    }
}
