using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Damage", fileName = "Damage")]
    public class DamageSpec : Spec
    {
        [SerializeField] private float bleed;
        [SerializeField] private bool loud = true;
        [SerializeField] private bool reputational = true;

        public float Bleed => bleed;

        public bool Loud => loud;

        public bool Reputational => reputational;
    }
}
