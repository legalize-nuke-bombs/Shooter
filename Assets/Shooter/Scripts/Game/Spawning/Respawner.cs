using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(RespawnPosition))]
    public class Respawner : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Health health;
        private Movement movement;
        private RespawnPosition respawnPosition;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<Movement>();
            respawnPosition = GetComponent<RespawnPosition>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RespawnRpc()
        {
            if (health.IsAlive) return;

            Vector3 position = respawnPosition.GetPosition();
            movement.Teleport(position);
            health.ResurrectWithFullHp();
            Log.Info($"Entity {name} respawned at {position}");
        }
    }
}
