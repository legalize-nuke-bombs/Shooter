using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "History Overflowing",
        category: "Conditions",
        story: "[Agent] history is overflowing",
        id: "e5aef589f53946d6aca15e7b10d3d586")]
    public partial class HistoryOverflowingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            return Agent.Value.GetComponent<LlmHistory>().IsOverflowing;
        }
    }
}
