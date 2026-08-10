using System;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class DefaultHunger : Hunger
    {
        [SerializeField] private float amount;
        [SerializeField] private float maxAmount;

        public override float Amount => amount;
        public override float MaxAmount => maxAmount;

        public override bool CanSpend(float a)
        {
            return amount >= a;
        }
        public override void Spend(float a)
        {
            a = Math.Max(a, 0);
            amount = Math.Max(amount - a, 0);
        }
        public override void Restore(float a)
        {
            amount = Math.Min(amount + a, maxAmount);
        }
    }
}
