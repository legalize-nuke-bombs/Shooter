using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.LookAroundEntity
{
    [RequireComponent(typeof(WorldDigester))]
    public class LookAroundEntityTool : LlmTool<LookAroundEntityArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private WorldDigester worldDigester;

        protected override void Awake()
        {
            base.Awake();
            worldDigester = GetComponent<WorldDigester>();
        }

        public override string Name => "look_around_entity";

        public override string Description => "This tool shows everything around entity whose ID was passed. The greater the distance, the less detail is visible.";

        protected override string Execute(LookAroundEntityArguments arguments)
        {
            Register<PersistentId> ids = Environment.Current.Registers.Of<PersistentId>();
            long targetId = arguments.TargetId;

            PersistentId id = ids.Of(targetId);
            if (id == null)
            {
                Log.Info($"Entity {name} tried to look around unknown entity");
                return $"Failed to find entity with ID {targetId}";
            }

            return $"Objects around entity {targetId}:\n" + worldDigester.Digest(id.transform.position);
        }
    }
}
