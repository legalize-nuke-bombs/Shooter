using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class FollowOrder : CustomOrder
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
            SaveData saveData = content.ToObject<SaveData>();
            TargetId = saveData.TargetId;
            Reach = saveData.Reach;
            Sprint = saveData.Sprint;
        }

        public Character Target
        {
            get
            {
                if (target == null) target = Character.FindById(TargetId, RegisterInactivePolicy.Exclude);

                return target != null && target.gameObject.activeInHierarchy ? target : null;
            }
        }

        public string TargetPromptName => "[ID " + TargetId + "]";

        public override bool IsDone(GameObject body)
        {
            return Target == null;
        }

        public override string GetPromptOutcome(GameObject body)
        {
            if (Target == null) return TargetPromptName + " is gone, you are no longer following it";

            AgentMovement movement = body.GetComponent<AgentMovement>();
            return movement.Status == AgentMovementStatus.Unreachable
                ? "There is no way toward " + TargetPromptName + " from where you stand, you are no longer following it"
                : "You are no longer following " + TargetPromptName;
        }

        protected override string GetPromptDescriptionWithoutStatus(GameObject body)
        {
            Character followed = Target;
            if (followed == null) return "Following " + TargetPromptName + ": it is gone";

            Vector3 targetPosition = followed.transform.position;
            return (Sprint ? "Running after " : "Following ") + TargetPromptName + " at " + Whereabouts.FormatCoordinates(targetPosition) + " keeping " + Mathf.RoundToInt(Reach) + " m from it: " + Mathf.RoundToInt(Vector3.Distance(targetPosition, body.transform.position)) + " m away";
        }
    }
}
