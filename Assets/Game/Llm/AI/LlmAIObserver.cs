using Shooter.Game.AI;
using Shooter.Game.AI.Bt;
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
        private BtReports btReports;

        private void Awake()
        {
            llm = GetComponent<Llm>();
            characterRelation = GetComponent<AICharacterRelation>();
            btReports = GetComponent<BtReports>();
        }

        private void OnEnable()
        {
            characterRelation.OnDamagedCallback += OnDamaged;
            btReports.OnReport += OnBtReport;
        }

        private void OnDisable()
        {
            characterRelation.OnDamagedCallback -= OnDamaged;
            btReports.OnReport -= OnBtReport;
        }

        private void OnBtReport(BtReport report)
        {
            Log.Info("OnBtReport");
            llm.Notice(report.Prompt, report.Urgent);
        }

        private void OnDamaged(AICharacterRelation.OnDamagedCallbackData data)
        {
            Log.Info("OnDamaged");
            llm.Notice($"Your character automatically worsened their attitude toward ID {data.AttackerId} by {data.RelationDelta} units because {data.AttackerId} dealt you {data.DamagePoints} x {data.DamageType.Id} damage", false);
        }
    }
}
