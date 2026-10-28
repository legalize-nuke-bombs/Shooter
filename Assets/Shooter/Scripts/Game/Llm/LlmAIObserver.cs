using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    public class LlmAIObserver : ShooterComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private Relations relations;
        private BehaviorReporter behaviorReporter;

        protected override void ShooterAwake()
        {
            llm = GetComponent<Llm>();
            relations = GetComponent<Relations>();
            behaviorReporter = GetComponent<BehaviorReporter>();
        }

        protected override void ShooterOnEnable()
        {
            relations.RelationWorsenedByDamage += HandleRelationWorsenedByDamage;
            behaviorReporter.Reported += HandleBehaviorReported;
        }

        protected override void ShooterOnDisable()
        {
            relations.RelationWorsenedByDamage -= HandleRelationWorsenedByDamage;
            behaviorReporter.Reported -= HandleBehaviorReported;
        }

        private void HandleBehaviorReported(BehaviorReport report)
        {
            Log.Info($"Entity {name} got a behavior report, urgent {report.IsUrgent}");
            llm.AppendEvent(report.Prompt, report.IsUrgent);
        }

        private void HandleRelationWorsenedByDamage(Relations.RelationWorsenedByDamageData worsening)
        {
            Log.Info($"Entity {name} worsened its relation to {worsening.AttackerId} for the damage it dealt");
            llm.AppendEvent($"Your character automatically worsened their attitude toward ID {worsening.AttackerId} by {Mathf.Abs(worsening.RelationDelta)} units because {worsening.AttackerId} dealt you {worsening.DamagePoints} x {worsening.DamageType.Id} damage", false);
        }
    }
}
