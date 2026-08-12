using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;

namespace Shooter.Game.Llm.GetWanderersIds
{
    public sealed class GetWanderersIdsTool : LlmTool<GetWanderersIdsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "get_wanderers_ids";

        public override string Description =>
            "Get a list of wanderers ids";

        protected override string Execute(GetWanderersIdsArguments arguments)
        {
            Register<Player> players = Environment.Current.Registers.Of<Player>();

            var sb = new StringBuilder();

            int found = 0;

            foreach (Player player in players.All)
            {
                PersistentId persistentId = player.GetComponent<PersistentId>();
                if (persistentId == null)
                {
                    Log.Warn($"Player {player.name} does not have persistent id");
                    continue;
                }

                sb.Append(persistentId.Value + " ");
                found++;
            }

            Log.Info($"Entity {name} requested wanderers ids. Found {found} wanderers");
            return sb.ToString();
        }
    }
}
