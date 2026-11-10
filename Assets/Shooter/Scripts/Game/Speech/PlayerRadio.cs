using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    public class PlayerRadio : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;
        private ConversationManager conversationManager;

        public event Action<IReadOnlyList<RadioPartner>> PartnersArrived;

        public event Action<long> PartnerOutOfReach;

        private void Awake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.Instance.Get<ConversationManager>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RequestPartnersRpc()
        {
            List<RadioPartner> partners = new();
            foreach (IConversationListener listener in Registers.Instance.GetAll<IConversationListener>(RegisterInactivePolicy.Include))
            {
                if (listener.CharacterId == character.Id) continue;

                Character partner = Character.FindById(listener.CharacterId, RegisterInactivePolicy.Include);
                if (ShooterEntity.GetComponentStatus(partner) == ShooterEntity.ComponentStatus.Disabled) continue;

                partners.Add(new RadioPartner { CharacterId = partner.Id, OutOfReach = !ConversationManager.CanHear(partner) });
            }

            Log.Info($"Player {OwnerClientId} is sent {partners.Count} radio partners");
            RaisePartnersArrivedRpc(partners.ToArray());
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(long partnerId, string speech)
        {
            if (string.IsNullOrEmpty(speech) || speech.Length > PlayerMouth.MaxSpeechLength)
            {
                Log.Info($"Player {OwnerClientId} radioed an empty or overlong line, ignored");
                return;
            }

            Character partner = Character.FindById(partnerId, RegisterInactivePolicy.Include);
            if (partner == null || partner == character || !partner.TryGetComponent(out IConversationListener _))
            {
                Log.Info($"Player {OwnerClientId} radioed {partnerId}, who is itself or hears no conversations, ignored");
                return;
            }

            if (!conversationManager.TrySay(character.Id, partnerId, speech, false, true)) RaisePartnerOutOfReachRpc(partnerId);
        }

        [Rpc(SendTo.Owner)]
        private void RaisePartnersArrivedRpc(RadioPartner[] partners)
        {
            PartnersArrived?.Invoke(partners);
        }

        [Rpc(SendTo.Owner)]
        private void RaisePartnerOutOfReachRpc(long partnerId)
        {
            PartnerOutOfReach?.Invoke(partnerId);
        }
    }
}
