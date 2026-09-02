using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Custom Order Changed",
        category: "Conditions",
        story: "[Agent] custom order has changed",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a06")]
    public partial class BtCustomOrderChangedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue customOrders;
        private BtCustomOrder seen;

        public override void OnStart()
        {
            customOrders = Agent.Value == null ? null : Agent.Value.GetComponent<BtCustomOrderQueue>();
            seen = customOrders == null ? null : customOrders.Current;
        }

        public override bool IsTrue()
        {
            return customOrders != null && !ReferenceEquals(customOrders.Current, seen);
        }
    }
}
