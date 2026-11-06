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
        name: "Write Game Object To Near Blackboards",
        story: "[Agent] writes game object [Variable] to near blackboards in radius [Radius], force if [Force]; include friends if [IncludeFriends], include neutrals if [IncludeNeutrals], include enemies if [IncludeEnemies]",
        category: "Action",
        id: "594cb4ba9a08476cbb726f54084f7d33")]
    public partial class WriteGameObjectToNearBlackboardsAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Variable;
        [SerializeReference] public BlackboardVariable<float> Radius;
        [SerializeReference] public BlackboardVariable<bool> Force;
        [SerializeReference] public BlackboardVariable<bool> IncludeFriends;
        [SerializeReference] public BlackboardVariable<bool> IncludeNeutrals;
        [SerializeReference] public BlackboardVariable<bool> IncludeEnemies;

        protected override Status OnStart()
        {
            Character character = Agent.Value.GetComponent<Character>();
            Relations relations = Agent.Value.GetComponent<Relations>();

            Vector3 position = Agent.Value.transform.position;

            foreach (Character target in Registers.Instance.Get<Chunks>().FindInRadius<Character>(position, Radius.Value, ChunkInactivePolicy.Exclude))
            {
                if (target == character) continue;
                if (target.TryGetComponent(out Health targetHealth) && !targetHealth.IsAlive) continue;
                if (!target.TryGetComponent(out BehaviorGraphAgent targetBtAgent)) continue;

                RelationStatus status = relations.GetStatus(target);
                if ((status == RelationStatus.Friend && !IncludeFriends.Value) || (status == RelationStatus.Neutral && !IncludeNeutrals.Value) || (status == RelationStatus.Enemy && !IncludeEnemies.Value))
                {
                    continue;
                }

                targetBtAgent.GetVariable(Variable.Name, out BlackboardVariable<GameObject> TargetValue);
                if (!Force.Value && TargetValue != null && TargetValue.Value != null)
                {
                    continue;
                }

                targetBtAgent.SetVariableValue(Variable.Name, Variable.Value);
            }

            return Status.Success;
        }
    }
}
