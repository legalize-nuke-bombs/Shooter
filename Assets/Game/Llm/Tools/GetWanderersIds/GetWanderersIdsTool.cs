using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Llm.ToolHelpers.Finder;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.GetWanderersIds
{
    [RequireComponent(typeof(WandererFinder))]
    public sealed class GetWanderersIdsTool : LlmTool<GetWanderersIdsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private WandererFinder wandererFinder;

        protected override void Awake()
        {
            base.Awake();
            wandererFinder = GetComponent<WandererFinder>();
        }

        public override string Name => "get_wanderers_ids";

        public override string Description =>
            "Get a list of wanderers ids";

        protected override string Execute(GetWanderersIdsArguments arguments)
        {
            var output = new FinderHashSetOutput();
            wandererFinder.Find(output);

            var sb = new StringBuilder();
            int found = 0;

            foreach (long targetId in output.All())
            {
                sb.Append(targetId + " ");
                found++;
            }

            return $"{found} wanderers: " + sb;
        }
    }
}
