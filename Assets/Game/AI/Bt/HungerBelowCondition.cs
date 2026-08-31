using System;
using Shooter.Game.Body;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game.AI.Bt
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Hunger Below",
        category: "Conditions",
        story: "[Agent] hunger is below [Threshold]",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a01")]
    public partial class HungerBelowCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Threshold = new(20f);

        public override bool IsTrue()
        {
            if (Agent.Value == null) return false;
            Hunger hunger = Agent.Value.GetComponent<Hunger>();
            return hunger != null && hunger.Amount < Threshold.Value;
        }
    }
}
