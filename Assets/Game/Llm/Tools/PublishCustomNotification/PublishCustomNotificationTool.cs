using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Llm.ToolHelpers.Finder;
using Shooter.Game.Notifying;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.PublishCustomNotification
{
    [RequireComponent(typeof(PersistentId))]
    [RequireComponent(typeof(CharacterFinder))]
    [RequireComponent(typeof(WandererFinder))]
    public class PublishCustomNotificationTool : LlmTool<PublishCustomNotificationArguments>
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

        public override string Name => "publish_custom_notification";

        public override string Description =>
            "Use this tool when you want to create and send a notification.";

        protected override string Execute(PublishCustomNotificationArguments arguments)
        {
            Log.Info($"Entity {name} is publishing notification...");

            IconSpec icon = Environment.Current.Icons.Of(arguments.IconName);
            if (icon == null)
            {
                Log.Warn($"Entity {name} tried to access unknown icon {arguments.IconName}");
                return $"Failed to publish: icon {arguments.IconName} does not exist";
            }

            EarSoundSpec sound = Environment.Current.EarSounds.Of(arguments.EarSoundName);
            if (sound == null)
            {
                Log.Warn($"Entity {name} tried to access unknown ear sound {arguments.EarSoundName}");
                return $"Failed to publish: ear sound {arguments.EarSoundName} does not exist";
            }

            Notification notification =
                new Notification("custom-notification")
                    .Under(icon)
                    .Along(sound)
                    .With("title", arguments.Title)
                    .With("subtitle", arguments.Subtitle);

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
            output.Exclude(id.Value);

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

                MainNotificationRecipient recipient = targetId.GetComponent<MainNotificationRecipient>();
                if (recipient == null)
                {
                    sb.Append($"ID {targetIdValue} does not accept notifications. ");
                    continue;
                }

                recipient.Receive(notification);
                published++;
            }

            sb.Append($"Published to {published} characters.");
            return sb.ToString();
        }
    }
}
