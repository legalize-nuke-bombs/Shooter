using System;

namespace Shooter.Game
{
    [Serializable]
    public sealed class SendMessageTool : SpeechTool
    {
        public override string Name => "send_message";

        public override string Description =>
            @"
Send a message over the radio to characters by their ids, residents and wanderers alike, wherever they are.
Talk to characters standing near you face to face with `say` instead.
Write to residents in English, and to a wanderer in the language he speaks.
Write to introduce yourself or share new information.

If you mark your message as urgent, residents will see it almost immediately.
If you don't mark it as urgent, residents will still see it, but only on their next LLM tick (e.g., when something urgent reaches them, such as a wanderer talking to them, or at the next interval tick).
Mark messages as urgent only when truly necessary.
Wanderers receive your messages immediately, regardless of the value of the `urgent` field.
";

        protected override bool Spoken => false;

        protected override bool CanHear(Character listener, out string refusal)
        {
            refusal = null;
            return true;
        }
    }
}
