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

        private MainTriggerable triggerable;

        protected virtual void Awake()
        {
            triggerable = GetComponent<MainTriggerable>();
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

        protected void OnTrigger(Character character)
        {
            Log.Info($"Entity {name} triggered on {character.name}");
            triggerable.OnTrigger(character);
        }
    }
}
