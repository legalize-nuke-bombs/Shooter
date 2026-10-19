using System;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class SayToWandererTool : LlmTool<SayToWandererArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private ConversationManager conversationManager;
        private Character ownCharacter;

        public override string Name => "say_to_wanderer";

        public override string Description =>
            "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.";

        protected override void HandleAttached()
        {
            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            ownCharacter = Self.GetComponent<Character>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have Character component required by tool {Name}");
            }
        }

        protected override string Execute(SayToWandererArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to say";

            Character wanderer = Character.FindById(arguments.WandererId, RegisterInactivePolicy.Exclude);
            if (wanderer == null || !wanderer.TryGetComponent(out Player _))
            {
                return $"Failed to find wanderer {arguments.WandererId}";
            }

            if (Vector3.Distance(ownCharacter.transform.position, wanderer.transform.position) > PlayerMouth.TalkReach)
            {
                return $"Wanderer {arguments.WandererId} is too far to hear you, use send_message to reach them over the radio";
            }

            conversationManager.Say(ownCharacter.Id, arguments.WandererId, arguments.Text, true, true);
            return $"Said to {arguments.WandererId}";
        }
    }
}
