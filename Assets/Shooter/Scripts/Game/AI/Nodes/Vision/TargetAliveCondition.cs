using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Target Alive",
        category: "Conditions",
        story: "[Target] Alive",
        id: "b991a37d1de147478488362960112691")]
    public partial class TargetAliveCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        public override bool IsTrue()
        {
            if (Target.Value == null)
            {
                return false;
            }
            if (Target.Value.TryGetComponent(out Health targetHealth))
            {
                return targetHealth.IsAlive;
            }
            return false;
        }
    }
}
