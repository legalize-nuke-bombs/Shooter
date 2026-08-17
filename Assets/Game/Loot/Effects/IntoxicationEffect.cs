using Shooter.Game.Body;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Intoxication", fileName = "Intoxication")]
    public class IntoxicationEffect : ItemEffect
    {
        [SerializeField] private ToxinSpec toxin;
        [SerializeField] private float force = 33f;

        public override void Apply(GameObject user)
        {
            Intoxication intoxication = user.transform.Find<Intoxication>();

            if (intoxication == null) return;

            intoxication.Intoxicate(toxin, force);
        }
    }
}
