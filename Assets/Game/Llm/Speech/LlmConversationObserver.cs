using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(Health))]
    public class LlmConversationObserver : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Character character;
        private ConversationManager conversations;
        private Health health;
        private Llm llm;

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
            character = GetComponent<Character>();
            health = GetComponent<Health>();
            llm = GetComponent<Llm>();
        }

        private void OnEnable()
        {
            conversations = GameState.Get<ConversationManager>();
            conversations.Said += Heard;
        }

        private void OnDisable()
        {
            if (conversations != null) conversations.Said -= Heard;
            conversations = null;
        }

        private void Heard(Conversation conversation, Message message)
        {
            if (message.AuthorId == character.Id || conversation.Partner(message.AuthorId) != character.Id) return;
            if (!health.Alive) return;

            Character author = Character.Of(message.AuthorId, Inactive.Include);

            if (author != null && author.TryGetComponent(out Player _))
            {
                llm.Notice(message.Spoken
                    ? $"Wanderer [ID {message.AuthorId}] says: {message.Content}"
                    : $"Wanderer [ID {message.AuthorId}] says over the radio: {message.Content}", message.Urgent);
                return;
            }

            Nameable nameable = author == null ? null : author.GetComponent<Nameable>();
            string authorName = nameable == null ? string.Empty : nameable.PromptName();

            llm.Notice($"[{Llm.Stamp()}] Mail from Character {message.AuthorId} ({authorName}): {message.Content}",
                message.Urgent);
            Log.Info($"Entity {name} got mail from {message.AuthorId}, urgent {message.Urgent}");
        }
    }
}
