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
        description: "Asks the legs, every tick it runs, for the point the reach short of the target; stands while the target is within the reach; aims afresh once the target has drifted two metres from where it was aimed at or the body has been moved by someone else. Fails while the body reports no way toward the target.",
        story: "[Agent] walks toward [Target] keeping [Reach]",
        category: "Action",
        id: "4f7a0c2e9b3d4a61b8e5d2c7f0a13b22")]
    public partial class WalkTowardTargetAction : Action
    {
        private const float TargetDrift = 2f;

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Transform> Target = new();
        [SerializeReference] public BlackboardVariable<float> Reach = new();
        [SerializeReference] public BlackboardVariable<bool> Sprint = new();

        private AgentMovement movement;
        private bool aimed;
        private Vector3 anchor;
        private Vector3 aim;

        protected override Status OnStart()
        {
            if (Agent.Value == null || Target.Value == null) return Status.Failure;

            if (movement == null)
            {
                movement = Agent.Value.GetComponent<AgentMovement>();
                if (movement == null) return Status.Failure;
            }

            Vector3 there = Target.Value.position;
            Vector3 feet = movement.Feet;
            var gap = new Vector3(feet.x - there.x, 0f, feet.z - there.z);

            if (gap.magnitude <= Reach.Value)
            {
                aimed = false;
                return Status.Success;
            }

            if (movement.Status == AgentMovementStatus.Displaced)
            {
                aimed = false;
                return Status.Success;
            }

            if (!aimed || Vector3.Distance(there, anchor) > TargetDrift)
            {
                anchor = there;
                aim = there + gap.normalized * Reach.Value;
                aimed = true;
            }

            movement.Walk(aim, Sprint.Value);
            return movement.Status == AgentMovementStatus.Unreachable ? Status.Failure : Status.Success;
        }
    }
}
