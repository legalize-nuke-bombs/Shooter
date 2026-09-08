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
        description: "Looks over the characters of the world for the nearest living one, other than the agent, within the radius; its transform goes into the variable; with the priority on, the nearest player within the radius wins over any nearer character. Fails when nobody is within the radius.",
        story: "[Agent] spots the nearest character within [Radius] into [Target], preferring players if [PlayerPriority]",
        category: "Action",
        id: "7d3b9e15c6a24f0d8b2e4c6a1f9d3e51")]
    public partial class SpotNearestCharacterAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Radius = new(30f);
        [SerializeReference] public BlackboardVariable<Transform> Target = new();
        [SerializeReference] public BlackboardVariable<bool> PlayerPriority = new(true);

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

            float nearestCharacter = Radius.Value * Radius.Value;
            Character spottedCharacter = null;

            float nearestPlayer = nearestCharacter;
            Player spottedPlayer = null;

            foreach (Character character in Registers.Current.Of<Character>(Inactive.Exclude))
            {
                if (character == self) continue;
                if (character.TryGetComponent(out Health health) && !health.Alive) continue;

                float apart = (character.transform.position - here).sqrMagnitude;

                if (apart < nearestCharacter)
                {
                    nearestCharacter = apart;
                    spottedCharacter = character;
                }

                if (PlayerPriority.Value && character.TryGetComponent(out Player player) && apart < nearestPlayer)
                {
                    nearestPlayer = apart;
                    spottedPlayer = player;
                }
            }

            if (spottedCharacter == null) return Status.Failure;

            if (PlayerPriority.Value && spottedPlayer != null)
            {
                Target.Value = spottedPlayer.transform;
            }
            else
            {
                Target.Value = spottedCharacter.transform;
            }

            return Status.Success;
        }
    }
}
