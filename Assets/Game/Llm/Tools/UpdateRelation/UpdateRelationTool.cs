using Shooter.Game.AI;
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
            @"
You have your own attitude towards every character, expressed by a number from 0 to 100: enemy, neutral, friend.
You automatically attack characters you consider enemies.
Your attitude drops automatically when somebody attacks you or your friends.

You can change the attitude at your discretion using this tool.
If you want to attack a character, change the attitude to zero.";

        protected override string Execute(UpdateRelationArguments arguments)
        {
            int old = characterRelation.Amount(arguments.TargetId);
            characterRelation.SetAmount(arguments.TargetId, arguments.Amount);

            return $"Your attitude to {arguments.TargetId}: {old} -> {arguments.Amount}";
        }
    }
}
