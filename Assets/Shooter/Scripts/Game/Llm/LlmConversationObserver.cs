using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    public class LlmConversationObserver : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;
        private ConversationManager conversationManager;
        private Health health;
        private Llm llm;

        protected override void ShooterAwake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.Get<ConversationManager>();
            health = GetComponent<Health>();
            llm = GetComponent<Llm>();
            enabled = NetworkManager.Singleton.IsServer;
        }

        protected override void ShooterOnEnable()
        {
            conversationManager.Said += Hear;
        }

        protected override void ShooterOnDisable()
        {
            conversationManager.Said -= Hear;
        }

        private void Hear(Conversation conversation, Message message)
        {
            if (message.AuthorId == character.Id || conversation.GetPartnerId(message.AuthorId) != character.Id) return;
            if (!health.IsAlive) return;

            Character author = Character.FindById(message.AuthorId, RegisterInactivePolicy.Include);

            if (author != null && author.TryGetComponent(out Player _))
            {
                llm.AppendEvent(message.Spoken
                    ? $"Wanderer [ID {message.AuthorId}] says: {message.Content}"
                    : $"Wanderer [ID {message.AuthorId}] says over the radio: {message.Content}", message.Urgent);
                return;
            }

            Nameable nameable = author == null ? null : author.GetComponent<Nameable>();
            string authorName = nameable == null ? string.Empty : nameable.GetPromptName();

            llm.AppendEvent($"[{Llm.FormatCurrentWorldTime()}] Mail from Character {message.AuthorId} ({authorName}): {message.Content}", message.Urgent);
            Log.Info($"Entity {name} got mail from {message.AuthorId}, urgent {message.Urgent}");
        }
    }
}
