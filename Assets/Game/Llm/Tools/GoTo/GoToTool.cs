using Shooter.Game.AI.Bt.CustomOrders;
using Shooter.Game.AI.Navigation;
using Shooter.Game.World;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(BtCustomOrderQueue))]
    [RequireComponent(typeof(Navigator))]
    public sealed class GoToTool : LlmTool<GoToArguments>
    {
        private const float GroundReach = 5f;

        private BtCustomOrderQueue customOrders;
        private Navigator navigator;

        public override string Name => "go_to";

        public override string Description =>
            @"
Walk in a direction for a distance by starting a second-level behavior tree action.
bearing: degrees clockwise from north, 0 north, 90 east, 180 south, 270 west; the number in parentheses next to everything you see.
distance: whole meters.
sprint: true to run; running burns stamina fast.
force: by default the call is refused while another second-level action is active; set force to true to drop it and start this one at once.
The result comes at once, the walk itself takes time: you will be notified when you arrive or when the way turns out blocked. Use look_at_yourself to check the active second-level action.
";

        protected override void Awake()
        {
            base.Awake();
            customOrders = GetComponent<BtCustomOrderQueue>();
            navigator = GetComponent<Navigator>();
        }

        protected override string Execute(GoToArguments arguments, LlmCallContext context)
        {
            if (arguments.Distance < 1) return "Distance must be at least 1 meter";

            int bearing = Cardinal.Bearing(arguments.Bearing);
            string label = $"the point {arguments.Distance} m {Cardinal.Side(bearing)} ({bearing}{Cardinal.Degree})";
            Vector3 target = transform.position + Quaternion.Euler(0f, bearing, 0f) * Vector3.forward * arguments.Distance;

            if (!NavMesh.SamplePosition(target, out NavMeshHit ground, GroundReach, NavMesh.AllAreas))
                return $"There is no walkable ground at {label}";
            if (!navigator.CanReach(ground.position))
                return $"There is no way from here to {label}";

            var order = new BtCoGoTo { Name = label, Destination = ground.position, Sprint = arguments.Sprint };
            string started = order.PromptDescription(gameObject);

            if (arguments.Force)
            {
                BtCustomOrder dropped = customOrders.Current;
                customOrders.ForcePut(order);
                return dropped == null
                    ? $"Started: {started}"
                    : $"Dropped: {dropped.PromptDescription(gameObject)}\nStarted: {started}";
            }

            if (customOrders.TryPut(order)) return $"Started: {started}";

            return $"Refused, another second-level action is active: {customOrders.Current.PromptDescription(gameObject)}\nCall again with force=true to replace it, or halt_bt to stop it";
        }
    }
}
