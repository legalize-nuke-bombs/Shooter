using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Player))]
    public class PlayerPresenceObserver : NetworkBehaviour, IToastSource
    {
        private const string EnteredTitleFormat = "{0} подключился";
        private const string LeftTitleFormat = "{0} отключился";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundAsset sound;

        private Player player;

        public event Action<Toast> ToastRaised;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) player.ControlChanged += HandleControlChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) player.ControlChanged -= HandleControlChanged;
        }

        private void HandleControlChanged()
        {
            bool entered = player.IsControlled;
            string shownName = NameMapper.GetShownName(player);

            int told = 0;
            foreach (PlayerPresenceObserver observer in Registers.Instance.GetAll<PlayerPresenceObserver>(RegisterInactivePolicy.Exclude))
            {
                if (observer == this || !observer.player.IsControlled) continue;

                observer.RaisePresenceToastRpc(shownName, player.PublicKey, entered);
                told++;
            }

            Log.Info($"Player {shownName} {(entered ? "entered" : "left")}, {told} players told");
        }

        [Rpc(SendTo.Owner)]
        private void RaisePresenceToastRpc(string shownName, string publicKey, bool entered)
        {
            string title = string.Format(entered ? EnteredTitleFormat : LeftTitleFormat, NameMapper.GetShownNameWithIdentity(shownName, publicKey));
            ToastRaised?.Invoke(new Toast(null, sound, title, null));
        }
    }
}
