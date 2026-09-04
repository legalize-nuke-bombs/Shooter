using System;
using Shooter.Game.Core;

namespace Shooter.Game.Llm.LookAtYourself
{
    [Serializable]
    public sealed class LookAtYourselfTool : LlmTool<LookAtYourselfArguments>
    {
        public override string Name => "look_at_yourself";

        public override string Description =>
            "Look at yourself: your own health, stamina, belongings and relations.";

        protected override void OnStart()
        {
        }

        protected override string Execute(LookAtYourselfArguments arguments, LlmCallContext context)
        {
            return "Your state:\n" + Digester.Current.Of(Self, DigestionDetail.Full);
        }
    }
}
