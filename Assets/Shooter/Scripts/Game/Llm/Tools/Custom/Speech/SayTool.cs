using System;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class SayTool : SpeechTool
    {
        public override string Name => "say";

        public override string Description =>
            @$"
Say something aloud to characters by their ids, residents and wanderers alike: only those within {ConversationManager.TalkReach} meters of you hear it.
This is how you talk face to face. Answer with this tool whoever talks to you face to face.
Reach characters who are farther away over the radio with `send_message`.
Speak to residents in English, and to a wanderer in the language he speaks.

If you mark your words as urgent, residents who hear them react almost immediately.
If you don't mark them as urgent, residents still hear them, but react only on their next LLM tick.
Mark your words as urgent only when the listener has to react right away.
Wanderers hear you immediately, regardless of the value of the `urgent` field.
";

        protected override bool Spoken => true;

        protected override bool CanHear(Character listener, out string refusal)
        {
            float distance = Vector3.Distance(OwnCharacter.transform.position, listener.transform.position);
            if (distance > ConversationManager.TalkReach)
            {
                refusal = $"{distance:F0} m away, too far to hear you, come within {ConversationManager.TalkReach} m or use send_message to reach them over the radio";
                return false;
            }
            if (listener.TryGetComponent(out Sleeper sleeper) && sleeper.IsSleeping)
            {
                refusal = "character is asleep and does not hear you";
                return false;
            }

            refusal = null;
            return true;
        }
    }
}
