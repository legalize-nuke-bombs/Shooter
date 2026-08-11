using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmHistory))]
    public sealed class LlmWaiting : MonoBehaviour
    {
        private readonly Dictionary<long, Action<string>> pending = new Dictionary<long, Action<string>>();
        private readonly List<long> presented = new List<long>();

        private LlmHistory history;

        public bool Any => pending.Count > 0;

        private void Awake()
        {
            history = GetComponent<LlmHistory>();
        }

        public void Listen(long wandererId, string message, Action<string> onAnswer)
        {
            pending[wandererId] = onAnswer;
            history.Arrive(new LlmMessage { Role = LlmRole.User, Content = $"Wanderer [ID {wandererId}] says: {message}" });
        }

        public bool Answer(long wandererId, string text)
        {
            if (!pending.Remove(wandererId, out Action<string> answer)) return false;

            answer(text);

            return true;
        }

        public void Snapshot()
        {
            presented.Clear();
            presented.AddRange(pending.Keys);
        }

        public void Silent()
        {
            foreach (long ignored in presented)
            {
                if (!pending.Remove(ignored, out Action<string> answer)) continue;

                answer(null);
                history.Append(new LlmMessage
                {
                    Role = LlmRole.User,
                    Content = $"Wanderer [ID {ignored}] got no answer from you and stopped waiting."
                });
            }
        }
    }
}
