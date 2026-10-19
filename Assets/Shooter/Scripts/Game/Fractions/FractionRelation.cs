using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Fraction Relation", fileName = "FractionRelation")]
    public class FractionRelation : Asset
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private Fraction fraction1;
        [SerializeField] private Fraction fraction2;
        [SerializeField] private int defaultAmount;

        public Fraction Fraction1 => fraction1;
        public Fraction Fraction2 => fraction2;
        public int DefaultAmount => defaultAmount;

        private void OnEnable()
        {
            if (fraction1 == null)
            {
                Log.Error($"Fraction relation {Id} does not have set fraction1");
            }
            if (fraction2 == null)
            {
                Log.Error($"Fraction relation {Id} does not have set fraction2");
            }
            if (defaultAmount < 0 || defaultAmount > 100)
            {
                Log.Error($"Fraction relation {Id} has bad default amount ({defaultAmount})");
            }
        }
    }
}
