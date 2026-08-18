using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(MainTriggerable))]
    public abstract class Trigger : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        [SerializeField] private bool allowReiteration = true;

        private readonly HashSet<long> done = new();

        private MainTriggerable triggerable;

        protected virtual void Awake()
        {
            triggerable = GetComponent<MainTriggerable>();
            if (triggerable == null) Log.Warn($"Entity {name} does not have main triggerable");

            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        protected void OnTrigger(CharacterId character)
        {
            if (!allowReiteration)
                if (!done.Add(character.Value))
                    return;

            Log.Info($"Entity {name} triggered on {character.name}");
            triggerable.OnTrigger(character);
        }
    }
}
