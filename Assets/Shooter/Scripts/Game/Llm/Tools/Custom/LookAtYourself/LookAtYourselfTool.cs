using System;

namespace Shooter.Game
{
    [Serializable]
    public sealed class LookAtYourselfTool : LlmTool<LookAtYourselfArguments>
    {
        public override string Name => "look_at_yourself";

        public override string Description =>
            "Look at yourself: your own health, stamina, belongings and relations.";

        protected override void Start()
        {
        }

        protected override string Execute(LookAtYourselfArguments arguments, LlmCallContext context)
        {
            return "Your state:\n" + Digester.DigestEntity(Self, DigestionDetail.Full);
        }
    }
}
