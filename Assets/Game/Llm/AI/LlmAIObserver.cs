using Shooter.Game.AI;
using Shooter.Game.AI.Eater;
using Shooter.Game.AI.Healer;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    public class LlmAIObserver : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private AIHealer healer;
        private AIEater eater;
        private AICharacterRelation characterRelation;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            healer = this.Find<AIHealer>();
            eater = this.Find<AIEater>();
            characterRelation = this.Find<AICharacterRelation>();
        }

        private void OnEnable()
        {
            healer.OnAutoHealCallback += OnAutoHeal;
            eater.OnAutoEatCallback += OnAutoEat;
            characterRelation.OnDamagedCallback += OnDamaged;
        }

        private void OnDisable()
        {
            healer.OnAutoHealCallback -= OnAutoHeal;
            eater.OnAutoEatCallback -= OnAutoEat;
            characterRelation.OnDamagedCallback -= OnDamaged;
        }

        private void OnAutoHeal(AIHealer.OnAutoHealCallbackData data)
        {
            Log.Info("OnAutoHeal");
            llm.Notice($"Your character automatically healed using {data.Item.Id} ({data.StartHp}hp -> {data.EndHp}hp)", false);
        }

        private void OnAutoEat(AIEater.OnAutoEatCallbackData data)
        {
            Log.Info("OnAutoEat");
            llm.Notice($"Your character automatically ate using {data.Item.Id} ({data.StartSaturation} saturation -> {data.EndSaturation} saturation)", false);
        }

        private void OnDamaged(AICharacterRelation.OnDamagedCallbackData data)
        {
            Log.Info("OnDamaged");
            llm.Notice($"Your character automatically worsened their attitude toward ID {data.AttackerId} by {data.RelationDelta} units because {data.AttackerId} dealt you {data.DamagePoints} x {data.DamageType.Id} damage", false);
        }
    }
}
