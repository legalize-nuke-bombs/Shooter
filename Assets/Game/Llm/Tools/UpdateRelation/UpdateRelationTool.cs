using Shooter.Game.AI;
using Shooter.Game.Core;

namespace Shooter.Game.Llm
{
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private AICharacterRelation aiCharacterRelation;

        public override string Name => "update_relation";

        public override string Description =>
            @"
Use this tool to update your relation to character.
If you want to attack a character, change the attitude to zero.";

        protected override void Awake()
        {
            base.Awake();
            aiCharacterRelation = this.Find<AICharacterRelation>();
        }

        protected override string Execute(UpdateRelationArguments arguments)
        {
            int old = aiCharacterRelation.Amount(arguments.TargetId);
            aiCharacterRelation.SetAmount(arguments.TargetId, arguments.Amount);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
