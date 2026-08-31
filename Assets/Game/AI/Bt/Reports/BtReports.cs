using System;
using UnityEngine;

namespace Shooter.Game.AI.Bt
{
    public class BtReports : MonoBehaviour
    {
        public event Action<BtReport> OnReport;

        public void Report(BtReport report)
        {
            OnReport?.Invoke(report);
        }
    }
}
