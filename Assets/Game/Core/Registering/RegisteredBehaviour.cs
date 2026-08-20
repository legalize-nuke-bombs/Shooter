using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class RegisteredBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Registers.Track(this);
        }

        protected virtual void OnDisable()
        {
            Registers.Untrack(this);
        }
    }
}
