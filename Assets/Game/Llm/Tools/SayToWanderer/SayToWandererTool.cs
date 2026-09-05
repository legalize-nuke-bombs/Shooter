using System;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;

namespace Shooter.Game.Llm.SayToWanderer
{
    [Serializable]
    public sealed class SayToWandererTool : LlmTool<SayToWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Character ownCharacter;
        private LlmPendingTable table;

        public override string Name => "say_to_wanderer";

        public override string Description =>
            "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.";

        public override bool Available => table.Any;

        protected override void OnStart()
        {
            ownCharacter = Self.GetComponent<Character>();
            table = Self.GetComponent<LlmPendingTable>();
            if (ownCharacter == null)
            {
                Log.Error($"Entity {Self.name} does not have Character component required by tool {Name}");
            }
            if (table == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmPendingTable component required by tool {Name}");
            }
        }

        protected override string Execute(SayToWandererArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to say";

            Character wanderer = Character.Of(arguments.WandererId, Inactive.Exclude);
            if (wanderer == null || !wanderer.TryGetComponent(out Player _))
            {
                return $"Failed to find wanderer {arguments.WandererId}";
            }

            ConversationManager conversations = ConversationManager.Current;
            if (conversations == null)
            {
                Log.Warn($"Entity {Self.name} can not speak: the world keeps no conversations");
                return "Failed to speak";
            }

            conversations.Say(ownCharacter.Id, arguments.WandererId, arguments.Text, true);
            return $"Said to {arguments.WandererId}";
        }
    }
}
