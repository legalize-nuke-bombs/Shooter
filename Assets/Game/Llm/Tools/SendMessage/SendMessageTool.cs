using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.SendMessage
{
    [Serializable]
    public sealed class SendMessageTool : LlmTool<SendMessageArguments>
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NotificationSpec mail;

        private Llm llm;
        private Character ownCharacter;
        private Nameable ownNameable;

        public override string Name => "send_message";

        public override string Description =>
            @"
Send a message to other characters by their ids.

This tool is your primary way of communicating with other residents.
Write to other residents in English.
Write to introduce yourself or share new information.

You can use this tool to communicate with the wanderers, but it is not the primary method of communicating with them.
The primary way to communicate with wanderers is the `say_to_wanderer` tool, which is available only when a wanderer has approached you and initiated a dialogue.
Use this tool to communicate with wanderers — much like a walkie-talkie.
Write to the wanderer in the language that they speak.
You never use this tool to reply to wanderers if one has specifically addressed you and is waiting for a response.

If you mark your message as urgent, other residents will see it almost immediately.
If you don't mark it as urgent, other residents will still see it, but only on the next LLM tick (e.g., upon receiving an urgent message, the next time a wanderer addresses them, or at the next interval tick).
Mark messages as urgent only when truly necessary.
Wanderers receive your messages immediately, regardless of the value of the `urgent` field.
";

        protected override void OnStart()
        {
            llm = Self.GetComponent<Llm>();
            ownCharacter = Self.GetComponent<Character>();
            ownNameable = Self.GetComponent<Nameable>();
            if (llm == null)
            {
                Log.Error($"Entity {Self.name} does not have component llm required by tool {Name}");
            }
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have component character required by tool {Name}");
            }
            if (ownNameable == null)
            {
                Log.Error($"Entity {Self.name} does not have component nameable required by tool {Name}");
            }
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
                var target = Character.Of(targetId, Inactive.Exclude);

                if (target == null)
                {
                    failed.Add($"{targetId} : character does not exist");
                    continue;
                }
                if (targetId == ownCharacter.Id)
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
                    .With("actorId", ownCharacter.Id)
                    .With(ownNameable == null ? new Arg("actorName", string.Empty) : ownNameable.NamedAs("actorName"))
                    .With("text", arguments.Content)
                    .Urgened(arguments.Urgent)
                );

                if (target.TryGetComponent(out Player player))
                {
                    llm.Answer(player.Id, arguments.Content);
                }

                delivered.Add(targetId);
                Log.Info($"Entity {Self.name} said to {targetId}: {arguments.Content}");
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
