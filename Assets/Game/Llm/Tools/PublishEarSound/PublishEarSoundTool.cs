using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Llm.ToolHelpers.Finder;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.PublishEarSound
{
    [RequireComponent(typeof(PersistentId))]
    [RequireComponent(typeof(CharacterFinder))]
    [RequireComponent(typeof(WandererFinder))]
    public class PublishEarSoundTool : LlmTool<PublishEarSoundArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private PersistentId id;
        private CharacterFinder characterFinder;
        private WandererFinder wandererFinder;

        protected override void Awake()
        {
            base.Awake();
            id = GetComponent<PersistentId>();
            characterFinder = GetComponent<CharacterFinder>();
            wandererFinder = GetComponent<WandererFinder>();
        }

        public override string Name => "publish_ear_sound";

        public override string Description =>
            "Use this tool when you want characters to hear a sound inside their heads.";

        protected override string Execute(PublishEarSoundArguments arguments)
        {
            Log.Info($"Entity {name} is publishing ear sound...");

            EarSoundSpec sound = Environment.Current.EarSounds.Of(arguments.EarSoundName);
            if (sound == null)
            {
                Log.Warn($"Entity {name} tried to access unknown ear sound {arguments.EarSoundName}");
                return $"Failed to publish: ear sound {arguments.EarSoundName} does not exist";
            }

            var output = new FinderHashSetOutput();
            if (arguments.IncludeEveryone)
            {
                characterFinder.Find(output);
            }
            if (arguments.IncludeEveryWanderer)
            {
                wandererFinder.Find(output);
            }
            foreach (long customId in arguments.IncludeCustomIds)
            {
                output.Include(customId);
            }

            var sb = new StringBuilder();
            int published = 0;

            Register<PersistentId> ids = Environment.Current.Registers.Of<PersistentId>();
            foreach (long targetIdValue in output.All())
            {
                PersistentId targetId = ids.Of(targetIdValue);
                if (targetId == null)
                {
                    sb.Append($"ID {targetIdValue} not found. ");
                    continue;
                }

                EarSpeaker speaker = targetId.GetComponent<EarSpeaker>();
                if (speaker == null)
                {
                    sb.Append($"ID {targetIdValue} does not have an ear speaker. ");
                    continue;
                }

                speaker.Play(sound);
                published++;
            }

            sb.Append($"Published to {published} characters.");
            return sb.ToString();
        }
    }
}
