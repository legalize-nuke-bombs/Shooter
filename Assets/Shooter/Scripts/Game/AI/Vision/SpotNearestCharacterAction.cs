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
        story: "[Agent] spots the nearest character within [Radius] into [Target], only spots enemies if [OnlyEnemies]",
        category: "Action",
        id: "66905ac8ab4b4e9bbf037cdb95590a5e")]
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

            foreach (Character character in  Registers.Instance.Get<Chunks>().FindInRadius<Character>(agentPosition, Radius.Value, ChunkInactivePolicy.Exclude))
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
