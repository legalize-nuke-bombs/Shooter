using Shooter.Game.Relationship;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(CharacterRelation))]
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private CharacterRelation characterRelation;

        protected override void Awake()
        {
            base.Awake();
            characterRelation = GetComponent<CharacterRelation>();
        }

        public override string Name => "update_relation";

        public override string Description =>
            "Change your absolute attitude toward the character (0 enemy, 100 friend). If you want to attack a character, change the attitude to zero.";

        protected override string Execute(UpdateRelationArguments arguments)
        {
            int old = characterRelation.Amount(arguments.TargetId);
            characterRelation.SetAmount(arguments.TargetId, arguments.Amount, arguments.Reason);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
