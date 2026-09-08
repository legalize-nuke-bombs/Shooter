using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.Eyes
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Spot Nearest Character",
        description: "Looks over the characters of the world for the nearest living one, other than the agent, within the radius; its transform goes into the variable. Fails when nobody is within the radius.",
        story: "[Agent] spots the nearest character within [Radius] into [Target]",
        category: "Action",
        id: "7d3b9e15c6a24f0d8b2e4c6a1f9d3e51")]
    public partial class SpotNearestCharacterAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Radius = new(30f);
        [SerializeReference] public BlackboardVariable<Transform> Target = new();

        private Character self;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            if (self == null)
            {
                self = Agent.Value.GetComponent<Character>();
                if (self == null) return Status.Failure;
            }

            Vector3 here = Agent.Value.transform.position;
            float nearest = Radius.Value * Radius.Value;
            Character spotted = null;

            foreach (Character character in Registers.Current.Of<Character>(Inactive.Exclude))
            {
                if (character == self) continue;
                if (character.TryGetComponent(out Health health) && !health.Alive) continue;

                float apart = (character.transform.position - here).sqrMagnitude;
                if (apart > nearest) continue;

                nearest = apart;
                spotted = character;
            }

            if (spotted == null) return Status.Failure;

            Target.Value = spotted.transform;
            return Status.Success;
        }
    }
}
