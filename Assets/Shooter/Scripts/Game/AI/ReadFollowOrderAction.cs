using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
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

        private CustomOrderSlot customOrderSlot;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (customOrderSlot == null)
            {
                customOrderSlot = Agent.Value.GetComponent<CustomOrderSlot>();
                if (customOrderSlot == null) return Status.Failure;
            }

            if (customOrderSlot.CurrentOrder is not FollowOrder order) return Status.Failure;

            Character followed = order.Target;
            if (followed == null) return Status.Failure;

            order.MarkRunning();
            Target.Value = followed.transform;
            Reach.Value = order.Reach;
            Sprint.Value = order.Sprint;
            return Status.Success;
        }
    }
}
