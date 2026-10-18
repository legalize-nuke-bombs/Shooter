using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class ShooterBehaviour : MonoBehaviour
    {
        protected void Awake()
        {
            Registers.Add(this);
            Awakened();
        }

        protected void OnDestroy()
        {
            Destroying();
            Registers.Remove(this);
        }

        protected virtual void Awakened()
        {
        }

        protected virtual void Destroying()
        {
        }
    }
}
