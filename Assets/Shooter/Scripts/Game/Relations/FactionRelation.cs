using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Faction Relation", fileName = "FactionRelation")]
    public class FactionRelation : Asset
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Faction faction1;
        [SerializeField] private Faction faction2;
        [SerializeField] private int defaultAmount;

        public Faction Faction1 => faction1;
        public Faction Faction2 => faction2;
        public int DefaultAmount => defaultAmount;

        private void OnEnable()
        {
            if (faction1 == null)
            {
                Log.Error($"Faction relation {Id} does not have set faction1");
            }
            if (faction2 == null)
            {
                Log.Error($"Faction relation {Id} does not have set faction2");
            }
            if (defaultAmount < 0 || defaultAmount > 100)
            {
                Log.Error($"Faction relation {Id} has bad default amount ({defaultAmount})");
            }
        }
    }
}
