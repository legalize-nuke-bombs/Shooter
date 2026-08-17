using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Damage", fileName = "Damage")]
    public class DamageSpec : Spec
    {
        [SerializeField] private bool loud = true;
        [SerializeField] private bool reputational = true;

        public bool Loud => loud;

        public bool Reputational => reputational;
    }
}
