using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Hunger Below",
        category: "Conditions",
        story: "[Agent] hunger is below [Threshold]",
        id: "49e196ccde4c4e1186f062d3463d9c63")]
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
