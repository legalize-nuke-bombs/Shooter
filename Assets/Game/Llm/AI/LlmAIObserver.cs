using Shooter.Game.AI;
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
        private AICharacterRelation characterRelation;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            characterRelation = GetComponent<AICharacterRelation>();
        }

        private void OnEnable()
        {
            characterRelation.OnDamagedCallback += OnDamaged;
        }

        private void OnDisable()
        {
            characterRelation.OnDamagedCallback -= OnDamaged;
        }

        private void OnDamaged(AICharacterRelation.OnDamagedCallbackData data)
        {
            Log.Info("OnDamaged");
            llm.Notice($"Your character automatically worsened their attitude toward ID {data.AttackerId} by {data.RelationDelta} units because {data.AttackerId} dealt you {data.DamagePoints} x {data.DamageType.Id} damage", false);
        }
    }
}
