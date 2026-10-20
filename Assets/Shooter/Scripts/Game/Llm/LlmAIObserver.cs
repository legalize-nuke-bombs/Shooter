using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    public class LlmAIObserver : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private AICharacterRelation characterRelation;
        private BehaviorReporter behaviorReporter;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            characterRelation = GetComponent<AICharacterRelation>();
            behaviorReporter = GetComponent<BehaviorReporter>();
        }

        private void OnEnable()
        {
            characterRelation.RelationWorsenedByDamage += HandleRelationWorsenedByDamage;
            behaviorReporter.Reported += HandleBehaviorReported;
        }

        private void OnDisable()
        {
            characterRelation.RelationWorsenedByDamage -= HandleRelationWorsenedByDamage;
            behaviorReporter.Reported -= HandleBehaviorReported;
        }

        private void HandleBehaviorReported(BehaviorReport report)
        {
            Log.Info($"Entity {name} got a behavior report, urgent {report.IsUrgent}");
            llm.AppendEvent(report.Prompt, report.IsUrgent);
        }

        private void HandleRelationWorsenedByDamage(AICharacterRelation.RelationWorsenedByDamageData worsening)
        {
            Log.Info($"Entity {name} worsened its relation to {worsening.AttackerId} for the damage it dealt");
            llm.AppendEvent($"Your character automatically worsened their attitude toward ID {worsening.AttackerId} by {worsening.RelationDelta} units because {worsening.AttackerId} dealt you {worsening.DamagePoints} x {worsening.DamageType.Id} damage", false);
        }
    }
}
