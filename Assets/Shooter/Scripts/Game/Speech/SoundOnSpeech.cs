using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnSpeech : ShooterNetworkBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Character character;
        private Speaker speaker;
        private ConversationManager conversationManager;

        protected override void ShooterAwake()
        {
            character = GetComponent<Character>();
            speaker = GetComponent<Speaker>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            conversationManager = Registers.Get<ConversationManager>();
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
