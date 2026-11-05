using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class GoToOrder : CustomOrder
    {
        public string TaskName { get; set; }
        public Vector3 Destination { get; set; }
        public bool Sprint { get; set; }

        public override string Kind => "go_to";
        private struct SaveData
        {
            public string Name { get; set; }
            public Vector3 Destination { get; set; }
            public bool Sprint { get; set; }
        }
        public override object SaveObject()
        {
            return new SaveData
            {
                Name = TaskName,
                Destination = Destination,
                Sprint = Sprint
            };
        }
        public override void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            TaskName = saveData.Name;
            Destination = saveData.Destination;
            Sprint = saveData.Sprint;
        }

        public override bool IsDone(GameObject body)
        {
            AgentMovement movement = body.GetComponent<AgentMovement>();
            if (movement.Target != Destination) return false;

            return movement.Status is AgentMovementStatus.Arrived or AgentMovementStatus.Unreachable or AgentMovementStatus.Displaced;
        }

        public override string GetPromptOutcome(GameObject body)
        {
            AgentMovement movement = body.GetComponent<AgentMovement>();
            switch (movement.Status)
            {
                case AgentMovementStatus.Unreachable:
                    return "Failed to find path to " + TaskName;
                case AgentMovementStatus.Displaced:
                    return "Your walk to " + TaskName + " was cut short: you have been moved somewhere else";
                default:
                    string shortfall = Whereabouts.DescribeShortfall(Destination - movement.FeetPosition);
                    return shortfall.Length == 0
                        ? "You have arrived at " + TaskName
                        : "You have arrived as close to " + TaskName + " as the ground allows, " + shortfall;
            }
        }

        protected override string GetPromptDescriptionWithoutStatus(GameObject body)
        {
            return (Sprint ? "Running" : "Walking") + " to " + TaskName + " at " + Whereabouts.FormatCoordinates(Destination) + ": " + Mathf.RoundToInt(Vector3.Distance(Destination, body.transform.position)) + " m left";
        }
    }
}
