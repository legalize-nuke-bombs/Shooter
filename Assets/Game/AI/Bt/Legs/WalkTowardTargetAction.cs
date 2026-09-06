using System;
using Shooter.Game.Body;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.Legs
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Walk Toward Target",
        description: "Keeps the legs headed for the point short of the target by the reach, re-planning when the target moved, planning afresh after an interruption or a displacement; fails only when there is no way.",
        story: "[Agent] walks toward [Target] keeping [Reach]",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a07")]
    public partial class WalkTowardTargetAction : Action
    {
        private const string TaskPrefix = "WalkToward ";
        private const float Replan = 2f;

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> Reach = new(3f);
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private BtWalk walk;
        private Vector3? planned;

        protected override Status OnStart()
        {
            if (Agent.Value == null || Target.Value == null) return Status.Failure;

            if (walk == null)
            {
                AgentMovement movement = Agent.Value.GetComponent<AgentMovement>();
                if (movement == null) return Status.Failure;

                walk = new BtWalk(movement, TaskPrefix);
            }

            if (walk.Take(out AgentMovement.CallbackData outcome))
            {
                switch (outcome.Status)
                {
                    case AgentMovementStatus.Unreachable:
                        return Status.Failure;
                    case AgentMovementStatus.Arrived:
                        break;
                    default:
                        planned = null;
                        break;
                }
            }

            Vector3 self = Agent.Value.transform.position;
            Vector3 target = Target.Value.transform.position;
            Vector3 toward = new Vector3(target.x - self.x, 0f, target.z - self.z);
            Vector3 point = target - toward.normalized * Mathf.Min(Reach.Value, toward.magnitude);

            if (planned == null || Vector3.Distance(point, planned.Value) > Replan)
            {
                planned = point;
                walk.Go(point, Sprint.Value);
            }

            return Status.Success;
        }
    }
}
