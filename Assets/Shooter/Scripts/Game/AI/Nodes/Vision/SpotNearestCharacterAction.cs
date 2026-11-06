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
        [SerializeReference] public BlackboardVariable<GameObject> Target = new();
        [SerializeReference] public BlackboardVariable<bool> OnlyEnemies = new(true);

        protected override Status OnStart()
        {
            Character character = Agent.Value.GetComponent<Character>();
            Relations relations = Agent.Value.GetComponent<Relations>();
            Vector3 position = Agent.Value.transform.position;

            float nearestSqrDistance = Radius.Value * Radius.Value;
            Character result = null;

            foreach (Character target in  Registers.Instance.Get<Chunks>().FindInRadius<Character>(position, Radius.Value, ChunkInactivePolicy.Exclude))
            {
                if (target == character) continue;
                if (target.TryGetComponent(out Health targetHealth) && !targetHealth.IsAlive) continue;

                float sqrDistance = (target.transform.position - position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;
                if (OnlyEnemies.Value && relations.GetStatus(target) != RelationStatus.Enemy) continue;

                nearestSqrDistance = sqrDistance;
                result = target;
            }

            if (result == null)
            {
                Target.Value = null;
                return Status.Failure;
            }

            Target.Value = result.gameObject;
            return Status.Success;
        }
    }
}
