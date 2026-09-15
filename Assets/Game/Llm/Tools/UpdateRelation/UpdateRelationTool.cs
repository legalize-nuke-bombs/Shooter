using System;
using Shooter.Game.AI;
using Shooter.Game.Core;
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

        protected override void OnStart()
        {
            aiCharacterRelation = Self.GetComponent<AICharacterRelation>();
            if (aiCharacterRelation == null)
            {
                Log.Error($"Entity {Self.name} does not have ai character relation component required by tool {Name}");
            }
        }


        protected override string Execute(UpdateRelationArguments arguments, LlmCallContext context)
        {
            var target = Character.Of(arguments.TargetId, Inactive.Include);
            if (target == null)
            {
                return $"Character ID {arguments.TargetId} does not exist.";
            }

            int old = aiCharacterRelation.Amount(target);
            aiCharacterRelation.SetAmount(target, arguments.Amount);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
