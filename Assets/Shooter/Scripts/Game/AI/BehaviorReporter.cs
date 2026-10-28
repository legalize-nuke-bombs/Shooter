using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class BehaviorReporter : ShooterComponent
    {
        public event Action<BehaviorReport> Reported;

        public void Report(BehaviorReport report)
        {
            Reported?.Invoke(report);
        }
    }
}
