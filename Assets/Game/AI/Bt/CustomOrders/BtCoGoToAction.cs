using System;
using Shooter.Game.Body;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Custom Order Go To",
        description: "Keeps the navigator on the current go_to custom order every tick: issues a new order, stops a cleared one, completes a finished one and reports the outcome.",
        story: "[Agent] follows the go_to custom order",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a05")]
    public partial class BtCoGoToAction : Action
    {
        private const string TaskPrefix = "BtCoGoTo ";

        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private BtCustomOrderQueue customOrders;
        private AgentMovement movement;
        private BtReports reports;
        private BtCoGoTo walking;
        private string task;
        private int issued;
        private AgentMovement.CallbackData? outcome;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;

            if (customOrders == null)
            {
                customOrders = Agent.Value.GetComponent<BtCustomOrderQueue>();
                movement = Agent.Value.GetComponent<AgentMovement>();
                reports = Agent.Value.GetComponent<BtReports>();
            }

            if (customOrders == null || movement == null) return Status.Failure;

            if (outcome != null) Settle();

            var wanted = customOrders.Current as BtCoGoTo;
            if (wanted == null)
            {
                Halt();
                return Status.Failure;
            }

            if (!ReferenceEquals(wanted, walking)) Issue(wanted);
            return Status.Success;
        }

        private void Issue(BtCoGoTo order)
        {
            issued++;
            task = TaskPrefix + issued;
            walking = order;
            outcome = null;
            order.Begin();
            movement.GoTo(task, order.Sprint, OnFinished, order.Destination);
        }

        private void Halt()
        {
            if (walking == null) return;

            if (movement.Status == AgentMovementStatus.Walking && movement.TaskName == task)
                movement.Interrupt("custom order cleared");

            task = null;
            walking = null;
            outcome = null;
        }

        private void OnFinished(AgentMovement.CallbackData data)
        {
            if (walking != null && data.TaskName == task) outcome = data;
        }

        private void Settle()
        {
            AgentMovement.CallbackData data = outcome.Value;
            BtCoGoTo order = walking;
            task = null;
            walking = null;
            outcome = null;

            switch (data.Status)
            {
                case AgentMovementStatus.Arrived:
                    Complete(order, data, Arrival(order, data));
                    return;
                case AgentMovementStatus.Unreachable:
                    Complete(order, data, $"Failed to find path to {order.Name}");
                    return;
                case AgentMovementStatus.Displaced:
                    Complete(order, data, $"Your walk to {order.Name} was cut short: you have been moved somewhere else");
                    return;
                default:
                    order.Suspend();
                    Log.Info($"Entity {Agent.Value.name} lost the way to {order.Name}: {data.Status} by {data.InterrupterName}");
                    return;
            }
        }

        private string Arrival(BtCoGoTo order, AgentMovement.CallbackData data)
        {
            string shortfall = Whereabouts.Shortfall(order.Destination - data.Position);
            return shortfall.Length == 0
                ? $"You have arrived at {order.Name}"
                : $"You have arrived as close to {order.Name} as the ground allows, {shortfall}";
        }

        private void Complete(BtCoGoTo order, AgentMovement.CallbackData data, string report)
        {
            bool cleared = customOrders.Complete(order);
            Log.Info($"Entity {Agent.Value.name} finished go_to custom order {order.Name}: {data.Status}, custom order cleared {cleared}");

            if (cleared && reports != null) reports.Report(new BtReport { Prompt = report, Urgent = true });
        }
    }
}
