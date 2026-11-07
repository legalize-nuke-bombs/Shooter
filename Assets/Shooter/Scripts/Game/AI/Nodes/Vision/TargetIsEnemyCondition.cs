using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Target Is Enemy",
        category: "Conditions",
        story: "[Agent] and [Target] are enemies",
        id: "627b089eb80143c1b8aee45411d030a7")]
    public partial class TargetIsEnemyCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        public override bool IsTrue()
        {
            if (Agent.Value == null || Target.Value == null)
            {
                return false;
            }
            if (Agent.Value.TryGetComponent(out Relations relations) && Target.Value.TryGetComponent(out Character targetCharacter))
            {
                return relations.GetStatus(targetCharacter) == RelationStatus.Enemy;
            }
            return false;
        }
    }
}
