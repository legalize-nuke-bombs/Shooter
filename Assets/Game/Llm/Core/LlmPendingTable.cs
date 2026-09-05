using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Speech;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Character))]
    public sealed class LlmPendingTable : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly HashSet<long> pending = new();

        private Character character;
        private ConversationManager conversations;

        public string ComponentKey => "LlmPendingTable";
        private struct SaveData
        {
            public List<long> Pending { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Pending = pending.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            pending.Clear();
            foreach (long id in sd.Pending) pending.Add(id);
        }

        public bool Any => pending.Count > 0;

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
            character = GetComponent<Character>();
        }

        private void OnEnable()
        {
            conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Entity {name} finds no conversations in the world, its debts will never settle by speech");
                return;
            }

            conversations.Said += Settle;
        }

        private void OnDisable()
        {
            if (conversations != null) conversations.Said -= Settle;
            conversations = null;
        }

        public List<long> Ids()
        {
            return pending.ToList();
        }

        public bool Has(long wandererId)
        {
            return pending.Contains(wandererId);
        }

        public void Mark(long wandererId)
        {
            pending.Add(wandererId);
        }

        public bool Clear(long wandererId)
        {
            return pending.Remove(wandererId);
        }

        private void Settle(Conversation conversation, Message message)
        {
            if (message.AuthorId != character.Id) return;

            long wandererId = conversation.Other(character.Id);
            if (pending.Remove(wandererId)) Log.Info($"Entity {name} has answered wanderer {wandererId}");
        }
    }
}
