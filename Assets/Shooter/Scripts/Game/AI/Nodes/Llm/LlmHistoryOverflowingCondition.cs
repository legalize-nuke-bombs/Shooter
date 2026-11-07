using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Llm History Overflowing",
        category: "Conditions",
        story: "[Agent] llm history is overflowing",
        id: "0fedc61075e940be9c725f0f2bb4d49c")]
    public partial class LlmHistoryOverflowingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            return Agent.Value.GetComponent<LlmHistory>().IsOverflowing;
        }
    }
}
