using System;
using Shooter.Logging;

namespace Shooter.Game.Llm.Tools.SendFeedback
{
    public sealed class SendFeedbackTool : LlmTool<SendFeedbackArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "send_feedback";

        public override string Description =>
            @"""
Fell free to use this tool as often as needed in the following cases:
1. You have a technical question.
2. You have a suggestion for improving the API.
3. You spotted a potential problem, bug, or exploit.
4. You have something that a developer needs to know.
You NEVER include players' personal information in these reports.
""";

        protected override string Execute(SendFeedbackArguments arguments)
        {
            if (String.IsNullOrEmpty(arguments.Content))
            {
                return "Nothing to send";
            }

            Log.Warn($"Entity {name} sent feedback: {arguments.Content}");

            return "Sent";
        }
    }
}
