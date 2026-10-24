using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(IncrementalId))]
    public class Player : ShooterNetworkBehaviour, ISaveableComponent
    {
        private const ulong NoClient = ulong.MaxValue;

        private readonly NetworkVariable<ulong> controllingClientId = new(NoClient);

        private IncrementalId incrementalId;

        public string PublicKey { get; set; }

        // Who plays this body, apart from who owns it in Netcode: on the host an offline body and a body being handed
        // to a client are both owned by the host without being its own
        public bool IsControlledLocally => IsSpawned && controllingClientId.Value == NetworkManager.LocalClientId;

        public event Action ControlChanged;

        private struct SaveData
        {
            public string PublicKey { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData { PublicKey = PublicKey };
        }

        public void LoadObject(SaveToken content)
        {
            PublicKey = content.ToObject<SaveData>().PublicKey;
        }

        protected override void ShooterAwake()
        {
            incrementalId = GetComponent<IncrementalId>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) return;

            controllingClientId.OnValueChanged += HandleControllingClientIdChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) return;

            controllingClientId.OnValueChanged -= HandleControllingClientIdChanged;
        }

        public void GiveControlTo(ulong clientId)
        {
            if (controllingClientId.Value == clientId) return;

            controllingClientId.Value = clientId;
            ControlChanged?.Invoke();
        }

        public void RevokeControl()
        {
            if (controllingClientId.Value == NoClient) return;

            controllingClientId.Value = NoClient;
            ControlChanged?.Invoke();
        }

        private void HandleControllingClientIdChanged(ulong previousClientId, ulong currentClientId)
        {
            ControlChanged?.Invoke();
        }

        public long Id => incrementalId.Value;

        public static Player FindById(long id, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Player player in Registers.GetAll<Player>(inactivePolicy))
            {
                if (player.Id == id)
                {
                    return player;
                }
            }
            return null;
        }

        public static Player FindByPublicKey(string publicKey, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Player player in Registers.GetAll<Player>(inactivePolicy))
            {
                if (player.PublicKey == publicKey)
                {
                    return player;
                }
            }
            return null;
        }
    }
}
