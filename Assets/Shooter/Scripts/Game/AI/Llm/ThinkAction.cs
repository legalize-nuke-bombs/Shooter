using System;
using System.Threading.Tasks;
using Shooter.Infrastructure;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Think",
        story: "[Agent] thinks",
        category: "Action",
        id: "fc1cad496aea4e96ac03364ed4ff132d")]
    public partial class ThinkAction : Action
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private Task<bool> thought;

        protected override Status OnStart()
        {
            thought = Agent.Value.GetComponent<Llm>().TryTick();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (!thought.IsCompleted) return Status.Running;

            if (thought.IsFaulted)
            {
                Log.Error($"Entity {Agent.Value.name} failed to think: {thought.Exception}");
            }

            return thought.IsCompletedSuccessfully && thought.Result ? Status.Success : Status.Failure;
        }
    }
}
