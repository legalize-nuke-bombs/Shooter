using System.Globalization;
using UnityEngine;

namespace Shooter.Game.AI
{
    [RequireComponent(typeof(AICharacterRelation))]
    public class AISettingDamageToReputationCoefficient : AISetting
    {
        private AICharacterRelation relation;

        private void Awake()
        {
            relation = GetComponent<AICharacterRelation>();
        }

        public override string Name => "damageToReputationCoefficient";
        public override string Range => "0.0 - 10.0";

        public override string Description => @"
            If this parameter is non-zero, then upon taking damage from a character, your relationship with them will worsen by the amount of damage received (excluding bleeding) multiplied by this coefficient.
            It is strongly recommended not to disable this setting or set it to an excessively low value, as doing so will prevent your character from automatically defending themselves in a timely manner when attacked.
         ";

        public override void Set(string content)
        {
            float amount = float.Parse(content, CultureInfo.InvariantCulture);
            relation.SetDamageToReputationCoefficient(amount);
        }

        public override string Get()
        {
            return relation.DamageToReputationCoefficient.ToString(CultureInfo.InvariantCulture);
        }
    }
}
