using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinSpec : Spec
    {
        [SerializeField] private float halfLife;

        public float HalfLife => halfLife;
    }
}
