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
        name: "Go To Order",
        description: "Walks to the point of the current go_to order, then completes the order and reports the outcome.",
        story: "[Agent] follows the go_to order",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a05")]
    public partial class GoToOrderAction : Action
    {
        private const string TaskName = "GoToOrder";

        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue orders;
        private Navigator navigator;
        private BtCoGoTo order;
        private Navigator.CallbackData? outcome;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            orders = Agent.Value.GetComponent<BtCustomOrderQueue>();
            navigator = Agent.Value.GetComponent<Navigator>();
            if (orders == null || navigator == null) return Status.Failure;

            order = orders.Current as BtCoGoTo;
            if (order == null) return Status.Failure;

            outcome = null;
            order.Begin();
            navigator.GoTo(TaskName, order.Sprint, OnFinished, order.Destination);
            return Judge();
        }

        protected override Status OnUpdate()
        {
            return Judge();
        }

        protected override void OnEnd()
        {
            if (order != null && orders.Current == order) order.Suspend();
            if (navigator != null && navigator.Status == NavigatorStatus.Walking && navigator.TaskName == TaskName)
                navigator.Interrupt("GoToOrder node ended");

            order = null;
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
                    Complete($"You have arrived at {order.Name}");
                    return Status.Success;
                case NavigatorStatus.Unreachable:
                    Complete($"You could not reach {order.Name}: there is no way there, the order is dropped");
                    return Status.Failure;
                default:
                    Log.Info($"Entity {Agent.Value.name} lost the way to {order.Name}: {outcome.Value.Status} by {outcome.Value.InterrupterName}");
                    return Status.Failure;
            }
        }

        private void Complete(string report)
        {
            bool cleared = orders.Complete(order);
            Log.Info($"Entity {Agent.Value.name} finished go_to {order.Name}: {outcome.Value.Status}, order cleared {cleared}");

            BtReports reports = Agent.Value.GetComponent<BtReports>();
            if (reports != null) reports.Report(new BtReport { Prompt = report, Urgent = true });
        }
    }
}
