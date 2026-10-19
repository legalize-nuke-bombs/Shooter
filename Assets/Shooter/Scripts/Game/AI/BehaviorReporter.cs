using System;
using UnityEngine;

namespace Shooter.Game
{
    public class BehaviorReporter : MonoBehaviour
    {
        public event Action<BehaviorReport> Reported;

        public void Report(BehaviorReport report)
        {
            Reported?.Invoke(report);
        }
    }
}
