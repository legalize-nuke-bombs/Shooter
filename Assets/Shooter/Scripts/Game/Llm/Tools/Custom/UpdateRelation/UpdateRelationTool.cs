using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class UpdateRelationTool : LlmTool<UpdateRelationArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Relations relations;

        public override string Name => "update_relation";

        public override string Description =>
            @"
Use this tool to update your relation to character.
This tool accepts absolute values, not relative ones.
If you want to attack a character, change the attitude to zero.
Always use this tool if a character does something good or bad to you.
";

        protected override void HandleAttached()
        {
            relations = Self.GetComponent<Relations>();
            if (relations == null)
            {
                Log.Error($"Entity {Self.name} does not have a relations component required by tool {Name}");
            }
        }

        protected override string Execute(UpdateRelationArguments arguments, LlmCallContext context)
        {
            Character target = Character.FindById(arguments.TargetId, RegisterInactivePolicy.Include);
            if (target == null)
            {
                return $"Character ID {arguments.TargetId} does not exist.";
            }

            int oldAmount = relations.GetAmount(target);
            relations.SetAmount(target, arguments.Amount);
            int newAmount = relations.GetAmount(target);

            return $"Your attitude to {arguments.TargetId}: {oldAmount} -> {newAmount}";
        }
    }
}
