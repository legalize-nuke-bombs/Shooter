using System;
using Shooter.Game.AI;
using Shooter.Logging;

namespace Shooter.Game.Llm.UpdateRelation
{
    [Serializable]
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private AICharacterRelation aiCharacterRelation;

        public override string Name => "update_relation";

        public override string Description =>
            @"
Use this tool to update your relation to character.
This tool accepts absolute values, not relative ones.
If you want to attack a character, change the attitude to zero.";

        public override void OnStart(LlmInitContext context)
        {
            aiCharacterRelation = context.Self.GetComponent<AICharacterRelation>();
            if (aiCharacterRelation == null)
            {
                Log.Error($"Entity {context.Self.name} does not have ai character relation component required by tool {Name}");
            }
        }


        protected override string Execute(UpdateRelationArguments arguments, LlmCallContext context)
        {
            int old = aiCharacterRelation.Amount(arguments.TargetId);
            aiCharacterRelation.SetAmount(arguments.TargetId, arguments.Amount);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
