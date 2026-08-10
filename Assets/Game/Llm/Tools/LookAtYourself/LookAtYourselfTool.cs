using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    public sealed class LookAtYourselfTool : LlmTool<LookAtYourselfArguments>
    {
        private Digester digester;

        protected override void Awake()
        {
            base.Awake();
            digester = GetComponent<Digester>();
        }

        public override string Name => "look_at_yourself";

        public override string Description =>
            "Look at yourself: your own health, stamina, belongings and relations.";

        protected override string Execute(LookAtYourselfArguments arguments)
        {
            return "Your state:\n" + digester.Of(gameObject, DigestionDetail.Full);
        }
    }
}
