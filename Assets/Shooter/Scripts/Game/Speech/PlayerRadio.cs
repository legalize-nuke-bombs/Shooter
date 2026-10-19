using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    public class PlayerRadio : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private Character character;
        private ConversationManager conversationManager;

        private void Awake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SayRpc(long partnerId, string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > PlayerMouth.SpeechLimit)
            {
                Log.Info($"Player {OwnerClientId} radioed an empty or overlong line, ignored");
                return;
            }

            if (conversationManager.GetIfPresent(character.Id, partnerId) == null)
            {
                Log.Info($"Player {OwnerClientId} radioed {partnerId} without ever talking to them, ignored");
                return;
            }

            conversationManager.Say(character.Id, partnerId, text, false, true);
        }
    }
}
