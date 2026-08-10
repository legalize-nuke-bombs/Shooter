using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class MainRestrainable : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private IRestraint[] restraints;

        public static readonly float InstantAction = 0.00000001f;

        public void Awake()
        {
            restraints = GetComponents<IRestraint>();
            Log.Info($"Entity {name} has {restraints.Length} restraints");
        }

        public bool CanPerform(ActionType type, float dt)
        {
            foreach (IRestraint restraint in restraints)
            {
                if (!restraint.CanPerform(type, dt))
                {
                    Log.Info($"Entity {name} cant perform {type}, reason: {restraint.GetType().Name}");
                    return false;
                }
            }
            return true;
        }

        public void RegisterAction(ActionType type, float dt)
        {
            foreach (IRestraint restraint in restraints)
            {
                restraint.RegisterAction(type, dt);
            }
        }
    }
}
