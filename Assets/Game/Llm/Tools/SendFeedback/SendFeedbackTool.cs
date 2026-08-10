using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using Shooter.Logging;

namespace Shooter.Game.Llm.Tools.SendFeedback
{
    public sealed class SendFeedbackTool : LlmTool<SendFeedbackArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "send_feedback";

        public override string Description =>
            @"""
Use this tool if you experience any of the following development or integration issues.
API CONFUSION: You don't understand the provided API, tools, or data structure.
BUG DETECTED: You found a system bug, broken logic, or weird text formatting.
FRICTION & INCONVENIENCE: The current workflow, prompt, or function feels clumsy, slow, or uncomfortable to use.
SUBOPTIMAL BEHAVIOR: Something works, but it is inefficient or could be done better.
IMPROVEMENT IDEA: You have a suggestion for the developer to optimize this integration.
You NEVER include players' personal data in these reports.
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
