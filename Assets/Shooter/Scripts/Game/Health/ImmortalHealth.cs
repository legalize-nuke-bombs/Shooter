using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(EarSpeaker))]
    public sealed class ImmortalHealth : Health
    {
        public override double Hp => 100;

        public override double MaxHp => 100;

        public override bool IsAlive => true;

        protected override void ReduceHp(double amount)
        {
        }

        public override void RestoreHp(double amount)
        {
        }

        public override void ResurrectWithFullHp()
        {
        }
    }
}
