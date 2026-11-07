using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Agent Displaced",
        category: "Conditions",
        story: "[Agent] was displaced",
        id: "6378d1e4ef984d4cbaf0011cb35c4500")]
    public partial class AgentDisplacedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            if (Agent.Value == null) return false;
            return Agent.Value.TryGetComponent(out AgentMovement movement) && movement.Status == AgentMovementStatus.Displaced;
        }
    }
}
