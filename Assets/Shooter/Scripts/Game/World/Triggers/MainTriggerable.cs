using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class MainTriggerable : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ITriggerable[] triggerables;

        protected override void ShooterAwake()
        {
            triggerables = GetComponents<ITriggerable>();
            Log.Info($"Entity {name} has {triggerables.Length} triggerables");
        }

        public void HandleCharacterEntered(Character character)
        {
            foreach (ITriggerable triggerable in triggerables)
            {
                triggerable.HandleCharacterEntered(character);
            }
        }
    }
}
