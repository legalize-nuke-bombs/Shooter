using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Game.World;
using Shooter.Logging;

namespace Shooter.Game.Llm.PublishCustomNotification
{
    public class PublishCustomNotificationTool : LlmTool<PublishCustomNotificationArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "publish_custom_notification";

        public override string Description =>
            @"
Use this tool when you want to create and send a notification.

`IconName` is the ID of your notification icon.
`EarSoundName` is the ID of your notification ear sound.
`Title` and `Subtitle` is the text of your notification. Write in English.

If you want everyone (both residents and strangers) to receive your notification, use the `IncludeEveryone` flag.
If you want the notification to be received only by specific character(s), pass their IDs to `IncludeCustomIds`.
";

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

            PersistentIds ids = Environment.Current.PersistentIds;

            if (arguments.IncludeEveryone)
            {
                return PublishIncludeEveryone(notification, ids);
            }

            return PublishCustomIds(notification, arguments.IncludeCustomIds, ids);
        }

        private string PublishIncludeEveryone(Notification notification, PersistentIds ids)
        {
            List<PersistentId> characters = ids.GetFiltered("Character");
            int published = 0;

            foreach (PersistentId character in characters)
            {
                MainNotificationRecipient recipient = character.GetComponent<MainNotificationRecipient>();
                if (recipient == null)
                {
                    continue;
                }

                recipient.Receive(notification);
                published++;
            }

            Log.Info($"Entity {name} published IncludeEveryone notification to {published} / {characters.Count} characters");
            return $"Published to {published} characters";
        }

        private string PublishCustomIds(Notification notification, long[] targetIds, PersistentIds ids)
        {
            var sb = new StringBuilder();
            int published = 0;

            foreach (long id in targetIds)
            {
                PersistentId character = ids.Of(id);
                if (character == null)
                {
                    sb.Append($"ID {id} not found. ");
                    continue;
                }

                MainNotificationRecipient recipient = character.GetComponent<MainNotificationRecipient>();
                if (recipient == null)
                {
                    sb.Append($"ID {id} does not accept notifications. ");
                    continue;
                }

                recipient.Receive(notification);
                published++;
            }

            sb.Append($"Published to {published} characters");
            string s = sb.ToString();

            Log.Info($"Entity {name} published IncludeCustomIds notification: {s}");
            return s;
        }
    }
}
