using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.ToolHelpers.Finder
{
    public class WandererFinder : MonoBehaviour, IFinder
    {
        private static readonly Journal Log = Logs.Here();

        public void Find(IFinderOutput output)
        {
            Register<Player> players = Registers.Current.Of<Player>();

            foreach (Player player in players.All)
            {
                PersistentId id = player.GetComponent<PersistentId>();
                if (id == null)
                {
                    Log.Warn($"Player {player.name} does not have persistent id");
                    continue;
                }

                output.Include(id.Value);
            }
        }
    }
}
