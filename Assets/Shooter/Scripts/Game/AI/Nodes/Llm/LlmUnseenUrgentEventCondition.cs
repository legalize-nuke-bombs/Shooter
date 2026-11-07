using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Llm Unseen Urgent Event",
        category: "Conditions",
        story: "[Agent] has an llm unseen urgent event",
        id: "7a09fa604f6f4e598d1322aaf84ace77")]
    public partial class LlmUnseenUrgentEventCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            return Agent.Value.GetComponent<LlmHistory>().HasUnseenUrgentEvent;
        }
    }
}
