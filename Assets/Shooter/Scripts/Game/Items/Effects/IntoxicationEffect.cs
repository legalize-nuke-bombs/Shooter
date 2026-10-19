using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Effects/Intoxication", fileName = "Intoxication")]
    public class IntoxicationEffect : ItemEffect
    {
        [SerializeField] private ToxinAsset toxin;
        [SerializeField] private float force = 33f;

        public override void ApplyToUser(GameObject user)
        {
            Intoxication intoxication = user.GetComponent<Intoxication>();

            if (intoxication == null) return;

            intoxication.AddToxin(toxin, force);
        }
    }
}
