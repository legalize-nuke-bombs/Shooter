using Shooter.Game.Core.Saves;
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

        protected override string PromptRawDescription()
        {
            return (Sprint ? "Running" : "Walking") + " to " + Destination + " (" + Name + ")";
        }
    }
}
