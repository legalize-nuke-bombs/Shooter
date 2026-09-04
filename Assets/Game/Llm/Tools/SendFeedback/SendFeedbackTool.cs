using System;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.SendFeedback
{
    [Serializable]
    public sealed class SendFeedbackTool : LlmTool<SendFeedbackArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private GameObject gameObject;

        public override string Name => "send_feedback";

        public override string Description =>
            @"Feel free to use this tool as often as needed in the following cases:
1. It seems to you that something in the prompts isn't formulated clearly enough.
2. It seems to you that one of your tools needs to be modified or removed.
3. It seems to you that you need additional tools.
4. You spotted a potential problem, bug, or exploit.
5. You have ANY other proposal for the development of this world.
The world is under active development; any feedback is valuable and will be read.
You NEVER include players' personal information in these reports.";

        public override void OnStart(LlmInitContext context)
        {
            gameObject = context.Self;
        }

        protected override string Execute(SendFeedbackArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Content)) return "Nothing to send";

            Log.Warn($"Entity {gameObject.name} sent feedback: {arguments.Content}");

            return "Sent";
        }
    }
}
