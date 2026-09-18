using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class RegisteredBehaviour : MonoBehaviour, IRegistered
    {
        private static readonly Journal Log = Logs.Here();

        protected virtual void Awake()
        {
            Log.Info($"Entity {name} is tracking");
            Registers.Track(this);
        }

        protected virtual void OnDestroy()
        {
            Log.Info($"Entity {name} is untracking");
            Registers.Untrack(this);
        }
    }
}
