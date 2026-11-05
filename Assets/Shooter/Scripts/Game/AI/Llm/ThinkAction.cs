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
        description: "Lets the resident's LLM think one turn: runs while the request is in flight, fails when the resident could not think now.",
        story: "[Agent] thinks",
        category: "Action",
        id: "9658fa10ff09452bb07bef1351bac27c")]
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
