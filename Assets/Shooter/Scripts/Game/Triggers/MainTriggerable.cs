using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class MainTriggerable : MonoBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private ITriggerable[] triggerables;

        private void Awake()
        {
            triggerables = GetComponents<ITriggerable>();
            Log.Info($"Entity {name} has {triggerables.Length} triggerables");
        }

        public void OnTrigger(Character character)
        {
            foreach (ITriggerable triggerable in triggerables) triggerable.OnTrigger(character);
        }
    }
}
