using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(OptimizerAgent))]
    public class OptimizerAgentSwitcherOnControl : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Player player;
        private OptimizerAgent agent;

        private void Awake()
        {
            player = GetComponent<Player>();
            agent = GetComponent<OptimizerAgent>();
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
        }

        private void HandleControlChanged()
        {
            bool keepsWorldAwake = IsServer ? player.IsControlled : player.IsControlledLocally;
            ShooterEntity.SetEnabled(agent, keepsWorldAwake);
            Log.Info($"Player {name} {(keepsWorldAwake ? "keeps" : "does not keep")} the world around it awake on this machine");
        }
    }
}
