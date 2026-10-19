using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    public class LlmConversationObserver : MonoBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private Character character;
        private ConversationManager conversationManager;
        private Health health;
        private Llm llm;

        private void Awake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            health = GetComponent<Health>();
            llm = GetComponent<Llm>();
            enabled = NetworkManager.Singleton.IsServer;
        }

        private void OnEnable()
        {
            conversationManager.Said += Hear;
        }

        private void OnDisable()
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
