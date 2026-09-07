using System;
using Shooter.Game.Core;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Read Follow Order",
        description: "Reads the follow custom order from the slot: the followed character, the reach and the pace go into the variables; fails when the slot holds no follow order or the character is gone.",
        story: "[Agent] reads the follow order into [Target], [Reach] and [Sprint]",
        category: "Action",
        id: "4f7a0c2e9b3d4a61b8e5d2c7f0a13b21")]
    public partial class ReadFollowOrderAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Transform> Target = new();
        [SerializeReference] public BlackboardVariable<float> Reach = new();
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

            if (customOrders.Current is not BtCoFollow order) return Status.Failure;

            Character followed = order.Target;
            if (followed == null) return Status.Failure;

            order.Begin();
            Target.Value = followed.transform;
            Reach.Value = order.Reach;
            Sprint.Value = order.Sprint;
            return Status.Success;
        }
    }
}
