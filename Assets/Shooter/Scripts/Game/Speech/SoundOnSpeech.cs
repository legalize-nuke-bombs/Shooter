using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnSpeech : NetworkBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Character character;
        private Speaker speaker;
        private ConversationManager conversationManager;

        private void Awake()
        {
            character = GetComponent<Character>();
            speaker = GetComponent<Speaker>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            conversationManager = Registers.Instance.Get<ConversationManager>();
            conversationManager.Said += PlayOnOwnSpokenLine;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            conversationManager.Said -= PlayOnOwnSpokenLine;
        }

        private void PlayOnOwnSpokenLine(Conversation conversation, Message message)
        {
            if (!message.Spoken || message.AuthorId != character.Id) return;

            speaker.Play(sound);
        }
    }
}
