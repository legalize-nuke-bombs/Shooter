using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Game.World;
using Shooter.Logging;

namespace Shooter.Game.Llm.PublishEarSound
{
    public class PublishEarSoundTool : LlmTool<PublishEarSoundArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "publish_ear_sound";

        public override string Description =>
            @"
Use this tool when you want the wanderers to hear a sound inside their heads.

If you want every wanderer to hear the sound, use the `IncludeEveryWanderer` flag.
If you want the sound to be heard only by specific character(s), pass their IDs to `IncludeCustomWanderers`.
";

        protected override string Execute(PublishEarSoundArguments arguments)
        {
            Log.Info($"Entity {name} is publishing ear sound...");

            EarSoundSpec sound = Environment.Current.EarSounds.Of(arguments.EarSoundName);
            if (sound == null)
            {
                Log.Warn($"Entity {name} tried to access unknown ear sound {arguments.EarSoundName}");
                return $"Failed to publish: ear sound {arguments.EarSoundName} does not exist";
            }


            var sb = new StringBuilder();

            if (arguments.IncludeEveryWanderer)
            {
                sb.AppendLine(PublishIncludeEveryWanderer(sound));
            }

            if (arguments.IncludeCustomWanderers != null && arguments.IncludeCustomWanderers.Length > 0)
            {
                sb.AppendLine(PublishCustomWanderers(sound, arguments.IncludeCustomWanderers));
            }

            return sb.ToString();
        }

        private string PublishIncludeEveryWanderer(EarSoundSpec sound)
        {
            int players = 0;
            int published = 0;

            foreach (Player player in Environment.Current.Registers.Of<Player>().All)
            {
                players++;

                EarSpeaker speaker = player.GetComponent<EarSpeaker>();
                if (speaker == null)
                {
                    Log.Warn($"Entity {player.name} does not have ear speaker");
                    continue;
                }

                speaker.Play(sound);

                published++;
            }

            Log.Info($"Entity {name} published IncludeEveryWanderer sound to {published} / {players} players");
            return $"Published to {published} players";
        }

        private string PublishCustomWanderers(EarSoundSpec sound, long[] targetIds)
        {
            Register<PersistentId> ids = Environment.Current.Registers.Of<PersistentId>();

            var sb = new StringBuilder();
            int published = 0;

            foreach (long targetId in targetIds)
            {
                PersistentId character = ids.Of(targetId);
                if (character == null)
                {
                    sb.Append($"ID {targetId} not found. ");
                    continue;
                }

                EarSpeaker speaker = character.GetComponent<EarSpeaker>();
                if (speaker == null)
                {
                    sb.Append($"ID {targetId} does not have an ear speaker. ");
                    continue;
                }

                speaker.Play(sound);
                published++;
            }

            sb.Append($"Published to {published} characters");
            string s = sb.ToString();
            Log.Info($"Entity {name} published IncludeCustomWanderers sound: {s}");
            return s;
        }
    }
}
