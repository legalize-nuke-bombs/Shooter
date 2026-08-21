using Shooter.Game.AI;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(AICharacterRelation))]
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private AICharacterRelation aiCharacterRelation;

        public override string Name => "update_relation";

        public override string Description =>
            @"
Use this tool to update your relation to character.
This tool accepts absolute values, not relative ones.
If you want to attack a character, change the attitude to zero.";

        protected override void Awake()
        {
            base.Awake();
            aiCharacterRelation = GetComponent<AICharacterRelation>();
        }

        protected override string Execute(UpdateRelationArguments arguments, LlmCallContext context)
        {
            int old = aiCharacterRelation.Amount(arguments.TargetId);
            aiCharacterRelation.SetAmount(arguments.TargetId, arguments.Amount);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
