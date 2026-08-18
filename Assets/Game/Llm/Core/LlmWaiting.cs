using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmHistory))]
    public sealed class LlmWaiting : MonoBehaviour
    {
        private readonly Dictionary<long, Action<string>> pending = new();

        private LlmHistory history;

        public bool Any => pending.Count > 0;

        private void Awake()
        {
            history = GetComponent<LlmHistory>();
        }

        public List<long> Ids()
        {
            var ids = new List<long>();
            foreach (long id in pending.Keys) ids.Add(id);
            return ids;
        }

        public bool IsWaiting(long id)
        {
            return pending.ContainsKey(id);
        }

        public void Listen(long wandererId, string message, Action<string> onAnswer)
        {
            pending[wandererId] = onAnswer;
            history.Arrive(new LlmMessage
            {
                Role = LlmRole.User,
                Content = $"Wanderer [ID {wandererId}] says: {message}"
            },
                true
            );
        }

        public bool Answer(long wandererId, string text)
        {
            if (!pending.Remove(wandererId, out Action<string> answer)) return false;

            answer(text);

            return true;
        }

        public void Forget(long wandererId)
        {
            pending.Remove(wandererId);
        }
    }
}
