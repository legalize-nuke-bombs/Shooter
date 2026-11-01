using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Factions/Faction", fileName = "Faction")]
    public class Faction : Asset
    {
        [SerializeField] private string promptName;
        public string PromptName => promptName;
    }
}
