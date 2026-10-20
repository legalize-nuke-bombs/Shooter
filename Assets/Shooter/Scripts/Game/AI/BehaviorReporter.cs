using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class BehaviorReporter : ShooterBehaviour
    {
        public event Action<BehaviorReport> Reported;

        public void Report(BehaviorReport report)
        {
            Reported?.Invoke(report);
        }
    }
}
