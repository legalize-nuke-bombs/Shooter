using System;
using UnityEngine;

namespace Shooter.Game
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
