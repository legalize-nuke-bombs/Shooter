using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Order Changed",
        category: "Conditions",
        story: "[Agent] order has changed",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a06")]
    public partial class OrderChangedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue orders;
        private BtCustomOrder seen;

        public override void OnStart()
        {
            orders = Agent.Value == null ? null : Agent.Value.GetComponent<BtCustomOrderQueue>();
            seen = orders == null ? null : orders.Current;
        }

        public override bool IsTrue()
        {
            return orders != null && !ReferenceEquals(orders.Current, seen);
        }
    }
}
