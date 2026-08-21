using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class RegisteredBehaviour : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        protected virtual void OnEnable()
        {
            Registers registers = Registers.Current;
            if (registers == null)
            {
                Log.Info($"Entity {name} will not be tracked because registers is not set");
                return;
            }
            Log.Info($"Entity {name} is tracking");
            Registers.Current.Track(this);
        }

        protected virtual void OnDisable()
        {
            Registers registers = Registers.Current;
            if (registers == null)
            {
                Log.Info($"Entity {name} will not be untracked because registers is not set");
                return;
            }
            Log.Info($"Entity {name} is untracking");
            Registers.Current.Untrack(this);
        }
    }
}
