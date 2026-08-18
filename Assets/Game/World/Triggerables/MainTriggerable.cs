using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class MainTriggerable : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ITriggerable[] triggerables;

        private void Awake()
        {
            triggerables = GetComponents<ITriggerable>();
            Log.Info($"Entity {name} has {triggerables.Length} triggerables");
        }

        public void OnTrigger(CharacterId character)
        {
            foreach (ITriggerable triggerable in triggerables) triggerable.OnTrigger(character);
        }
    }
}
