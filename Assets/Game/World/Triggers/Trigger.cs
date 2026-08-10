using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(MainTriggerable))]
    public abstract class Trigger : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private MainTriggerable triggerable;

        private readonly HashSet<long> done = new HashSet<long>();
        [SerializeField] private bool allowReiteration = true;

        protected virtual void Awake()
        {
            triggerable = GetComponent<MainTriggerable>();
            if (triggerable == null)
            {
                Log.Warn($"Entity {name} does not have main triggerable");
            }
        }

        protected void OnTrigger(PersistentId character)
        {
            if (!allowReiteration)
            {
                if (!done.Add(character.Value))
                {
                    return;
                }
            }

            Log.Info($"Entity {name} triggered on {character.name}");
            triggerable.OnTrigger(character);
        }
    }
}
