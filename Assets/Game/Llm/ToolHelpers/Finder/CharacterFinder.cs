using Shooter.Game.Core;
using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.Llm.ToolHelpers.Finder
{
    public class CharacterFinder : MonoBehaviour, IFinder
    {
        public void Find(IFinderOutput output)
        {
            Register<PersistentId> ids = Registers.Current.Of<PersistentId>();
            int characterLayer = LayerMask.NameToLayer("Character");

            foreach (PersistentId id in ids.All)
            {
                if (id.gameObject.layer == characterLayer)
                {
                    output.Include(id.Value);
                }
            }
        }
    }
}
