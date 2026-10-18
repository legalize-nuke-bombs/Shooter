using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class RegisteredBehaviour : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        protected virtual void Awake()
        {
            Registers.Add(this);
            Log.Info($"Entity {name} registered");
        }

        protected virtual void OnDestroy()
        {
            Registers.Remove(this);
            Log.Info($"Entity {name} unregistered");
        }
    }
}
