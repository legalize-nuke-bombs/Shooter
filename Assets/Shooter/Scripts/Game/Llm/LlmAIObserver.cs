using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    public class LlmAIObserver : MonoBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

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
            characterRelation.RelationWorsenedByDamage += OnDamaged;
            behaviorReporter.Reported += OnBtReport;
        }

        private void OnDisable()
        {
            characterRelation.RelationWorsenedByDamage -= OnDamaged;
            behaviorReporter.Reported -= OnBtReport;
        }

        private void OnBtReport(BehaviorReport report)
        {
            Log.Info("OnBtReport");
            llm.Notice(report.Prompt, report.IsUrgent);
        }

        private void OnDamaged(AICharacterRelation.RelationWorsenedByDamageData data)
        {
            Log.Info("OnDamaged");
            llm.Notice($"Your character automatically worsened their attitude toward ID {data.AttackerId} by {data.RelationDelta} units because {data.AttackerId} dealt you {data.DamagePoints} x {data.DamageType.Id} damage", false);
        }
    }
}
