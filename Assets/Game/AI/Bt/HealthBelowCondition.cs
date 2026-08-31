using System;
using Shooter.Game.Body;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game.AI.Bt
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Health Below",
        category: "Conditions",
        story: "[Agent] health is below [Threshold]",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a02")]
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
