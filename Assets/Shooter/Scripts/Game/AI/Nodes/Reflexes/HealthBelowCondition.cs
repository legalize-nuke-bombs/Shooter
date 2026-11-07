using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Health Below",
        category: "Conditions",
        story: "[Agent] health is below [Threshold]",
        id: "7cf5bd4217d549d3a515321fa470286c")]
    public partial class HealthBelowCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Threshold = new(95f);

        public override bool IsTrue()
        {
            if (Agent.Value == null) return false;
            Health health = Agent.Value.GetComponent<Health>();
            return health != null && health.Hp < Threshold.Value;
        }
    }
}
