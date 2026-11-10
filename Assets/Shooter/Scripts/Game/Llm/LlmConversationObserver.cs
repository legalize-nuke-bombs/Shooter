using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    public class LlmConversationObserver : MonoBehaviour, IConversationListener
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;
        private ConversationManager conversationManager;
        private Health health;
        private Llm llm;

        public long CharacterId => character.Id;

        private void Awake()
        {
            character = GetComponent<Character>();
            conversationManager = Registers.Instance.Get<ConversationManager>();
            health = GetComponent<Health>();
            llm = GetComponent<Llm>();
            ShooterEntity.SetEnabled(this, NetworkManager.Singleton.IsServer);
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

            string verb = message.Spoken ? "says" : "says over the radio";
            llm.AppendEvent($"[{Llm.FormatCurrentWorldTime()}] {DescribeAuthor(message.AuthorId)} {verb}: {message.Content}", message.Urgent);
            Log.Info($"Entity {name} heard {message.AuthorId}, spoken {message.Spoken}, urgent {message.Urgent}");
        }

        private static string DescribeAuthor(long authorId)
        {
            Character author = Character.FindById(authorId, RegisterInactivePolicy.Include);
            if (author != null && author.TryGetComponent(out Player _)) return $"Wanderer [ID {authorId}]";

            Nameable nameable = author == null ? null : author.GetComponent<Nameable>();
            string authorName = nameable == null ? string.Empty : nameable.GetPromptName();
            return $"Character {authorId} ({authorName})";
        }
    }
}
