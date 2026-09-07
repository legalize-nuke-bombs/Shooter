using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    public class BtCoFollow : BtCustomOrder
    {
        public long TargetId { get; set; }
        public float Reach { get; set; }
        public bool Sprint { get; set; }

        private Character target;

        public override string Kind => "follow";
        private struct SaveData
        {
            public long TargetId { get; set; }
            public float Reach { get; set; }
            public bool Sprint { get; set; }
        }
        public override object SaveObject()
        {
            return new SaveData
            {
                TargetId = TargetId,
                Reach = Reach,
                Sprint = Sprint
            };
        }
        public override void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            TargetId = sd.TargetId;
            Reach = sd.Reach;
            Sprint = sd.Sprint;
        }

        public Character Target
        {
            get
            {
                if (target == null) target = Character.Of(TargetId, Inactive.Exclude);

                return target != null && target.gameObject.activeInHierarchy ? target : null;
            }
        }

        public string Handle => "[ID " + TargetId + "]";

        public override bool Done(GameObject body)
        {
            return Target == null;
        }

        public override string PromptOutcome(GameObject body)
        {
            return Handle + " is gone, you are no longer following it";
        }

        protected override string PromptRawDescription(GameObject body)
        {
            Character followed = Target;
            if (followed == null) return "Following " + Handle + ": it is gone";

            Vector3 there = followed.transform.position;
            return (Sprint ? "Running after " : "Following ") + Handle + " at " + Whereabouts.Coordinates(there) + " keeping " + Mathf.RoundToInt(Reach) + " m from it: " + Mathf.RoundToInt(Vector3.Distance(there, body.transform.position)) + " m away";
        }
    }
}
