using Shooter.Game.Body;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    public class BtCoGoTo : BtCustomOrder
    {
        public string Name { get; set; }
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
                Name = Name,
                Destination = Destination,
                Sprint = Sprint
            };
        }
        public override void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Name = sd.Name;
            Destination = sd.Destination;
            Sprint = sd.Sprint;
        }

        public override bool Done(GameObject body)
        {
            AgentMovement movement = body.GetComponent<AgentMovement>();
            if (movement.Target != Destination) return false;

            return movement.Status is AgentMovementStatus.Arrived or AgentMovementStatus.Unreachable or AgentMovementStatus.Displaced;
        }

        public override string PromptOutcome(GameObject body)
        {
            AgentMovement movement = body.GetComponent<AgentMovement>();
            switch (movement.Status)
            {
                case AgentMovementStatus.Unreachable:
                    return "Failed to find path to " + Name;
                case AgentMovementStatus.Displaced:
                    return "Your walk to " + Name + " was cut short: you have been moved somewhere else";
                default:
                    string shortfall = Whereabouts.Shortfall(Destination - movement.Feet);
                    return shortfall.Length == 0
                        ? "You have arrived at " + Name
                        : "You have arrived as close to " + Name + " as the ground allows, " + shortfall;
            }
        }

        protected override string PromptRawDescription(GameObject body)
        {
            return (Sprint ? "Running" : "Walking") + " to " + Name + " at " + Whereabouts.Coordinates(Destination) + ": " + Mathf.RoundToInt(Vector3.Distance(Destination, body.transform.position)) + " m left";
        }
    }
}
