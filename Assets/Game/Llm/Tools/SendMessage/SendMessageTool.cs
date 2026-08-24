using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Character))]
    public sealed class SendMessageTool : LlmTool<SendMessageArguments>
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec mail;

        private Character ownId;
        private Nameable ownNameable;

        public override string Name => "send_message";

        public override string Description =>
            @"
Send a message to other characters by their ids.

This tool is your primary way of communicating with other residents.
Write to other residents in English.
Each of your messages triggers that resident's LLM tick, so write only to introduce yourself or share new information.

You can use this tool to communicate with the wanderers, but it is not the primary method of communicating with them.
The primary way to communicate with wanderers is the `say_to_wanderer` tool, which is available only when a wanderer has approached you and initiated a dialogue.
Use this method to communicate with wanderers like a walkie-talkie to share new information or call them over for a face-to-face conversation.
Write to the wanderer in the language that, to the best of your knowledge, they speak.
";

        protected override void Awake()
        {
            base.Awake();
            ownId = GetComponent<Character>();
            ownNameable = GetComponent<Nameable>();
        }

        protected override string Execute(SendMessageArguments arguments, LlmCallContext context)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content))
            {
                return "Nothing to send";
            }

            var delivered = new List<long>();
            var failed = new List<string>();

            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                var target = Character.Of(targetId);

                if (target == null)
                {
                    failed.Add($"{targetId} : character does not exist");
                    continue;
                }
                if (targetId == ownId.Value)
                {
                    failed.Add($"{targetId}: it's you");
                    continue;
                }
                if (!target.TryGetComponent(out Health health) || !health.Alive)
                {
                    failed.Add($"{targetId}: character is dead");
                    continue;
                }
                if (!target.TryGetComponent(out MainNotificationRecipient recipient))
                {
                    failed.Add($"{targetId}: character does not have main notification recipient");
                    continue;
                }

                recipient.Receive(mail.Notify()
                    .With("actorId", ownId.Value)
                    .With(ownNameable == null ? new Arg("actorName", string.Empty) : ownNameable.NamedAs("actorName"))
                    .With("text", arguments.Content));
                delivered.Add(targetId);
                Log.Info($"Entity {name} said to {targetId}: {arguments.Content}");
            }

            var answer = new StringBuilder();
            if (delivered.Count > 0)
            {
                answer.Append("Delivered to ").Append(string.Join(", ", delivered));
            }
            foreach (string failure in failed)
            {
                if (answer.Length > 0)
                {
                    answer.Append('\n');
                }
                answer.Append("Not delivered to ").Append(failure);
            }

            return answer.ToString();
        }
    }
}
