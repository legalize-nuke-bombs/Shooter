using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Speech
{
    [RequireComponent(typeof(Character))]
    public class PlayerRadio : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(long partnerId, string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > PlayerMouth.SpeechLimit)
            {
                Log.Info($"Player {OwnerClientId} radioed an empty or overlong line, ignored");
                return;
            }

            ConversationManager conversations = GameState.Get<ConversationManager>();
            if (conversations.GetIfPresent(character.Id, partnerId) == null)
            {
                Log.Info($"Player {OwnerClientId} radioed {partnerId} without ever talking to them, ignored");
                return;
            }

            conversations.Say(character.Id, partnerId, text, false, true);
        }
    }
}
