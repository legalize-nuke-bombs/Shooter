using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(PersistentId))]
    public sealed class SendMessageTool : LlmTool<SendMessageArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private PersistentId ownId;

        protected override void Awake()
        {
            base.Awake();
            ownId = GetComponent<PersistentId>();
        }

        public override string Name => "send_message";

        public override string Description =>
            "Send a message to other residents by their ids. Write in English. Message a resident to pass something new.";

        protected override string Execute(SendMessageArguments arguments)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content))
            {
                return "Nothing to send";
            }

            var delivered = new List<long>();
            var failed = new List<string>();

            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                PersistentId target = Environment.Current.PersistentIds.Of(targetId);

                if (target == null || target.gameObject == gameObject || !target.TryGetComponent<Llm>(out _))
                {
                    failed.Add($"{targetId}: no resident bears this id");
                    continue;
                }

                if (!target.TryGetComponent(out Health health) || !health.Alive)
                {
                    failed.Add($"{targetId}: the resident is dead");
                    continue;
                }

                if (!target.TryGetComponent(out MainNotificationRecipient recipient))
                {
                    failed.Add($"{targetId}: the resident hears nothing");
                    continue;
                }

                recipient.Receive(new MailNotification(ownId.Value, arguments.Content));
                delivered.Add(targetId);
                Log.Info($"Entity {name} said to {targetId}: {arguments.Content}");
            }

            var answer = new StringBuilder();
            if (delivered.Count > 0) answer.Append("Delivered to ").Append(string.Join(", ", delivered));
            foreach (string failure in failed)
            {
                if (answer.Length > 0) answer.Append('\n');
                answer.Append("Not delivered to ").Append(failure);
            }

            return answer.ToString();
        }
    }
}
