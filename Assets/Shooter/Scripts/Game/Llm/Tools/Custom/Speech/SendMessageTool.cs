using System;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;

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
Send a message over the radio to characters by their ids, residents and wanderers alike, wherever they are.
Talk to characters standing near you face to face with `say` instead.
Write to residents in English, and to a wanderer in the language he speaks.
Write to introduce yourself or share new information.
";

        protected override void Start()
        {
            conversationManager = Registers.Instance.Get<ConversationManager>();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have Character component required by tool {Name}");
            }
        }

        protected override string Execute(SendMessageArguments arguments, LlmCallContext context)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content)) return "Nothing to say";

            StringBuilder result = new();
            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                if (result.Length > 0) result.Append('\n');

                string refusal = FindRefusal(targetId);
                if (refusal != null)
                {
                    result.Append($"Not said to {targetId}: {refusal}");
                    continue;
                }

                conversationManager.Say(ownCharacter.Id, targetId, arguments.Content, false, arguments.Urgent);
                result.Append($"Said to {targetId}");
            }

            return result.ToString();
        }

        private string FindRefusal(long targetId)
        {
            Character target = Character.FindById(targetId, RegisterInactivePolicy.Exclude);
            if (target == null) return "character does not exist";
            if (targetId == ownCharacter.Id) return "it's you";
            if (!target.TryGetComponent(out Health health) || !health.IsAlive) return "character is dead";
            return null;
        }
    }
}
