using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class MainRestrainable : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static readonly float InstantActionDuration = 0.00000001f;

        private IRestraint[] restraints;

        protected override void ShooterAwake()
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
                    Log.Debug($"Entity {name} can not perform {type}, reason: {restraint.GetType().Name}");
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
