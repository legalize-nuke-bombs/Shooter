using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class SendMessageTool : LlmTool<SendMessageArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private ConversationManager conversationManager;
        private Character ownCharacter;

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

        protected override void HandleAttached()
        {
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have component character required by tool {Name}");
            }
        }

        protected override string Execute(SendMessageArguments arguments, LlmCallContext context)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content))
            {
                return "Nothing to send";
            }

            var deliveredIds = new List<long>();
            var failures = new List<string>();

            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                Character target = Character.FindById(targetId, RegisterInactivePolicy.Exclude);

                if (target == null)
                {
                    failures.Add($"{targetId} : character does not exist");
                    continue;
                }
                if (targetId == ownCharacter.Id)
                {
                    failures.Add($"{targetId}: it's you");
                    continue;
                }
                if (!target.TryGetComponent(out Health health) || !health.IsAlive)
                {
                    failures.Add($"{targetId}: character is dead");
                    continue;
                }
                conversationManager.Say(ownCharacter.Id, targetId, arguments.Content, false, arguments.Urgent);

                deliveredIds.Add(targetId);
                Log.Info($"Entity {Self.name} said to {targetId}: {arguments.Content}");
            }

            StringBuilder result = new();
            if (deliveredIds.Count > 0)
            {
                result.Append("Delivered to ").Append(string.Join(", ", deliveredIds));
            }
            foreach (string failure in failures)
            {
                if (result.Length > 0)
                {
                    result.Append('\n');
                }
                result.Append("Not delivered to ").Append(failure);
            }

            return result.ToString();
        }
    }
}
