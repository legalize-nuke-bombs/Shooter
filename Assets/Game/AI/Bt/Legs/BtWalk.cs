using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.AI.Bt.Legs
{
    public class BtWalk
    {
        private readonly AgentMovement movement;
        private readonly string taskPrefix;

        private string task;
        private int issued;
        private Vector3 destination;
        private bool sprint;
        private AgentMovement.CallbackData? outcome;

        public BtWalk(AgentMovement movement, string taskPrefix)
        {
            this.movement = movement;
            this.taskPrefix = taskPrefix;
        }

        public bool Walking => task != null;

        public void Go(Vector3 destination, bool sprint)
        {
            if (task != null && this.destination == destination && this.sprint == sprint) return;

            issued++;
            task = taskPrefix + issued;
            this.destination = destination;
            this.sprint = sprint;
            outcome = null;
            movement.GoTo(task, sprint, OnFinished, destination);
        }

        public void Stop(string reason)
        {
            if (task == null) return;

            if (movement.Status == AgentMovementStatus.Walking && movement.TaskName == task) movement.Interrupt(reason);

            task = null;
            outcome = null;
        }

        public bool Take(out AgentMovement.CallbackData data)
        {
            if (outcome == null)
            {
                data = default;
                return false;
            }

            data = outcome.Value;
            task = null;
            outcome = null;
            return true;
        }

        private void OnFinished(AgentMovement.CallbackData data)
        {
            if (task != null && data.TaskName == task) outcome = data;
        }
    }
}
