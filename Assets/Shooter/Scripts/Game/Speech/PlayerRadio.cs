using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    public class PlayerRadio : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;
        private ConversationManager conversationManager;

        protected override void ShooterAwake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.Instance.Get<ConversationManager>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(long partnerId, string speech)
        {
            if (string.IsNullOrEmpty(speech) || speech.Length > PlayerMouth.MaxSpeechLength)
            {
                Log.Info($"Player {OwnerClientId} radioed an empty or overlong line, ignored");
                return;
            }

            if (conversationManager.GetIfPresent(character.Id, partnerId) == null)
            {
                Log.Info($"Player {OwnerClientId} radioed {partnerId} without ever talking to them, ignored");
                return;
            }

            conversationManager.Say(character.Id, partnerId, speech, false, true);
        }
    }
}
