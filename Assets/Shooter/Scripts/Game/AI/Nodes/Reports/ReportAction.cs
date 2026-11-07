using System;
using System.Text.RegularExpressions;
using Shooter.Infrastructure;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Report",
        story: "[Agent] reports [Message], urgent [Urgent]",
        category: "Action",
        id: "3182298628b847c3a6ffd1282c8879ce")]
    public partial class ReportAction : Action
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly Regex VariableReference = new(@"\{(\w+)\}");

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<string> Message = new();
        [SerializeReference] public BlackboardVariable<bool> Urgent = new();

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            BehaviorReporter behaviorReporter = Agent.Value.GetComponent<BehaviorReporter>();
            BehaviorGraphAgent graphAgent = Agent.Value.GetComponent<BehaviorGraphAgent>();
            if (behaviorReporter == null || graphAgent == null) return Status.Failure;

            string prompt = VariableReference.Replace(Message.Value, reference => FormatVariable(graphAgent, reference.Groups[1].Value) ?? reference.Value);
            behaviorReporter.Report(new BehaviorReport { Prompt = prompt, IsUrgent = Urgent.Value });
            return Status.Success;
        }

        private static string FormatVariable(BehaviorGraphAgent graphAgent, string variableName)
        {
            if (!graphAgent.GetVariable(variableName, out BlackboardVariable variable))
            {
                Log.Error($"Entity {graphAgent.name} reports unknown variable {variableName}");
                return null;
            }

            object value = variable.ObjectValue;
            if (value is UnityEngine.Object unityObject && unityObject == null) return "nothing";

            return value switch
            {
                Vector3 position => Whereabouts.FormatCoordinates(position),
                GameObject entity => FormatEntity(entity),
                float number => Mathf.RoundToInt(number).ToString(),
                null => "nothing",
                _ => value.ToString()
            };
        }

        private static string FormatEntity(GameObject entity)
        {
            IncrementalId incrementalId = entity.GetComponent<IncrementalId>();
            if (incrementalId != null) return "ID " + incrementalId.Value;

            Log.Error($"Entity {entity.name} is reported without IncrementalId");
            return "nothing";
        }
    }
}
