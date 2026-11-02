using System;
using System.Linq;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class SayTool : LlmTool<SayArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private ConversationManager conversationManager;
        private Character ownCharacter;

        public override string Name => "say";

        public override string Description =>
            @$"
Say something aloud to characters by their ids, residents and wanderers alike: only those within {ConversationManager.TalkReach} meters of you hear it.
This is how you talk face to face. Answer with this tool whoever talks to you face to face.
Reach characters who are farther away over the radio with `send_message`.
Speak to residents in English, and to a wanderer in the language he speaks.
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

        protected override string Execute(SayArguments arguments, LlmCallContext context)
        {
            if (arguments.TargetIds == null || arguments.TargetIds.Length == 0 || string.IsNullOrEmpty(arguments.Content)) return "Nothing to say";

            StringBuilder result = new();
            foreach (long targetId in arguments.TargetIds.Distinct())
            {
                if (result.Length > 0) result.Append('\n');

                string refusal = FindRefusal(targetId);
                if (refusal == null && !conversationManager.TrySay(ownCharacter.Id, targetId, arguments.Content, true, arguments.Urgent)) refusal = "too far to hear you";
                if (refusal != null)
                {
                    result.Append($"Not said to {targetId}: {refusal}");
                    continue;
                }

                result.Append($"Said to {targetId}");
            }

            return result.ToString();
        }

        private string FindRefusal(long targetId)
        {
            Character target = Character.FindById(targetId, RegisterInactivePolicy.Include);
            if (target == null) return "character does not exist";
            if (targetId == ownCharacter.Id) return "it's you";
            if (!target.TryGetComponent(out Health health) || !health.IsAlive) return "character is dead";
            if (ShooterEntity.GetComponentStatus(target) == ShooterEntity.ComponentStatus.Disabled) return "character does not exist";

            float distance = Vector3.Distance(ownCharacter.transform.position, target.transform.position);
            if (distance > ConversationManager.TalkReach)
            {
                return $"{distance:F0} m away, too far to hear you, come within {ConversationManager.TalkReach} m or use send_message to reach them over the radio";
            }

            if (target.TryGetComponent(out Sleeper sleeper) && sleeper.IsSleeping) return "character is asleep and does not hear you";
            return null;
        }
    }
}
