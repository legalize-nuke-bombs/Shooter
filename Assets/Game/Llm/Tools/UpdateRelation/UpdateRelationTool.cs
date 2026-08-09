using Shooter.Game.Relationship;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(CharacterRelation))]
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private CharacterRelation characterRelation;

        private void Awake()
        {
            characterRelation = GetComponent<CharacterRelation>();
        }

        public override string Name => "update_relation";

        public override string Description =>
            "Change your attitude to a character (0 enemy, 100 friend).";

        protected override string Execute(UpdateRelationArguments arguments)
        {
            int old = characterRelation.Amount(arguments.TargetId);
            characterRelation.SetAmount(arguments.TargetId, arguments.Amount, arguments.Reason);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
