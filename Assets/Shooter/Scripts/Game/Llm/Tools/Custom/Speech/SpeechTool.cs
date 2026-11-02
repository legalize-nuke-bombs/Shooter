using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public abstract class SpeechTool : LlmTool<SpeechArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private ConversationManager conversationManager;

        protected Character OwnCharacter { get; private set; }

        protected abstract bool Spoken { get; }

        protected override void Start()
        {
            conversationManager = Registers.Instance.Get<ConversationManager>();
            OwnCharacter = Self.GetComponent<Character>();
            if (OwnCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have Character component required by tool {Name}");
            }
        }

        protected override string Execute(SpeechArguments arguments, LlmCallContext context)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content))
            {
                return "Nothing to say";
            }

            var listenerIds = new List<long>();
            var refusals = new List<string>();

            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                if (!CanSayTo(targetId, out string refusal))
                {
                    refusals.Add($"{targetId}: {refusal}");
                    continue;
                }

                conversationManager.Say(OwnCharacter.Id, targetId, arguments.Content, Spoken, arguments.Urgent);
                listenerIds.Add(targetId);
            }

            StringBuilder result = new();
            if (listenerIds.Count > 0)
            {
                result.Append("Said to ").Append(string.Join(", ", listenerIds));
            }
            foreach (string refusal in refusals)
            {
                if (result.Length > 0)
                {
                    result.Append('\n');
                }
                result.Append("Not said to ").Append(refusal);
            }

            return result.ToString();
        }

        protected abstract bool CanHear(Character listener, out string refusal);

        private bool CanSayTo(long targetId, out string refusal)
        {
            Character target = Character.FindById(targetId, RegisterInactivePolicy.Exclude);
            if (target == null)
            {
                refusal = "character does not exist";
                return false;
            }
            if (targetId == OwnCharacter.Id)
            {
                refusal = "it's you";
                return false;
            }
            if (!target.TryGetComponent(out Health health) || !health.IsAlive)
            {
                refusal = "character is dead";
                return false;
            }

            return CanHear(target, out refusal);
        }
    }
}
