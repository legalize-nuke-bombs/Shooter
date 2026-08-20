using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class RegisteredBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Registers.Current.Track(this);
        }

        protected virtual void OnDisable()
        {
            Registers world = Registers.Current;
            if (world != null) world.Untrack(this);
        }
    }
}
