using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Target In Radius",
        category: "Conditions",
        story: "[Agent] sees [Target] in radius [Radius]",
        id: "fcbec3c282ef42038614fcb6c455f483")]
    public partial class TargetInRadiusCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> Radius;

        public override bool IsTrue()
        {
            if (Agent.Value == null || Target.Value == null)
            {
                return false;
            }
            return Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position) < Radius;
        }
    }
}
