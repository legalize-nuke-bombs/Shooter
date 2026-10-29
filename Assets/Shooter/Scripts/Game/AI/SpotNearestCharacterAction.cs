using System;
using Shooter.Infrastructure;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Spot Nearest Character",
        description: "Looks over the characters of the world for the nearest living one, other than the agent, within the radius; its transform goes into the variable; with only enemies on, it passes over the characters the agent does not count as enemies. Fails when nobody is within the radius.",
        story: "[Agent] spots the nearest character within [Radius] into [Target], only spots enemies if [OnlyEnemies]",
        category: "Action",
        id: "7d3b9e15c6a24f0d8b2e4c6a1f9d3e51")]
    public partial class SpotNearestCharacterAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Radius = new(30f);
        [SerializeReference] public BlackboardVariable<Transform> Target = new();
        [SerializeReference] public BlackboardVariable<bool> OnlyEnemies = new(true);

        private Character ownCharacter;
        private Relations relations;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            if (ownCharacter == null) ownCharacter = Agent.Value.GetComponent<Character>();
            if (relations == null) relations = Agent.Value.GetComponent<Relations>();

            Vector3 agentPosition = Agent.Value.transform.position;

            float nearestSqrDistance = Radius.Value * Radius.Value;
            Character spottedCharacter = null;

            foreach (Character character in Chunks.Instance.FindInRadius<Character>(agentPosition, Radius.Value, ChunkInactivePolicy.Exclude))
            {
                if (character == ownCharacter) continue;
                if (character.TryGetComponent(out Health health) && !health.IsAlive) continue;

                float sqrDistance = (character.transform.position - agentPosition).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;
                if (OnlyEnemies.Value && relations.GetStatus(character) != RelationStatus.Enemy) continue;

                nearestSqrDistance = sqrDistance;
                spottedCharacter = character;
            }

            if (spottedCharacter == null) return Status.Failure;

            Target.Value = spottedCharacter.transform;
            return Status.Success;
        }
    }
}
