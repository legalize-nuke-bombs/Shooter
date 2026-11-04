using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Unseen Urgent Event",
        category: "Conditions",
        story: "[Agent] has an unseen urgent event",
        id: "c373886665cf4d8c87a0e3b5a9aa3550")]
    public partial class UnseenUrgentEventCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            return Agent.Value.GetComponent<LlmHistory>().HasUnseenUrgentEvent;
        }
    }
}
