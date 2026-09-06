using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game.AI.Bt.Legs
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Target Farther Than",
        category: "Conditions",
        story: "[Target] is farther than [Distance] from [Agent]",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a06")]
    public partial class TargetFartherThanCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> Distance = new(4f);

        public override bool IsTrue()
        {
            if (Agent.Value == null || Target.Value == null) return false;

            return Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position) > Distance.Value;
        }
    }
}
