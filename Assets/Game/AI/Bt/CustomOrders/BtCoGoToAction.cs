using System;
using Shooter.Game.AI.Navigation;
using Shooter.Logging;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Custom Order: Go To",
        description: "Walks to the point of the current go_to custom order, then completes the custom order and reports the outcome.",
        story: "[Agent] follows the go_to custom order",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a05")]
    public partial class BtCoGoToAction : Action
    {
        private const string TaskName = "BtCoGoTo";

        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue customOrders;
        private Navigator navigator;
        private BtCoGoTo customOrder;
        private Navigator.CallbackData? outcome;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            customOrders = Agent.Value.GetComponent<BtCustomOrderQueue>();
            navigator = Agent.Value.GetComponent<Navigator>();
            if (customOrders == null || navigator == null) return Status.Failure;

            customOrder = customOrders.Current as BtCoGoTo;
            if (customOrder == null) return Status.Failure;

            outcome = null;
            customOrder.Begin();
            navigator.GoTo(TaskName, customOrder.Sprint, OnFinished, customOrder.Destination);
            return Judge();
        }

        protected override Status OnUpdate()
        {
            return Judge();
        }

        protected override void OnEnd()
        {
            if (customOrder != null && customOrders.Current == customOrder) customOrder.Suspend();
            if (navigator != null && navigator.Status == NavigatorStatus.Walking && navigator.TaskName == TaskName)
                navigator.Interrupt("BtCoGoTo node ended");

            customOrder = null;
            outcome = null;
        }

        private void OnFinished(Navigator.CallbackData data)
        {
            outcome = data;
        }

        private Status Judge()
        {
            if (outcome == null) return Status.Running;

            switch (outcome.Value.Status)
            {
                case NavigatorStatus.Arrived:
                    Complete($"You have arrived at {customOrder.Name}");
                    return Status.Success;
                case NavigatorStatus.Unreachable:
                    Complete($"You could not reach {customOrder.Name}: there is no way there, the order is dropped");
                    return Status.Failure;
                default:
                    Log.Info($"Entity {Agent.Value.name} lost the way to {customOrder.Name}: {outcome.Value.Status} by {outcome.Value.InterrupterName}");
                    return Status.Failure;
            }
        }

        private void Complete(string report)
        {
            bool cleared = customOrders.Complete(customOrder);
            Log.Info($"Entity {Agent.Value.name} finished go_to custom order {customOrder.Name}: {outcome.Value.Status}, custom order cleared {cleared}");

            BtReports reports = Agent.Value.GetComponent<BtReports>();
            if (reports != null) reports.Report(new BtReport { Prompt = report, Urgent = true });
        }
    }
}
