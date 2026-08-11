using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.PublishCustomNotification
{
    [RequireComponent(typeof(PersistentId))]
    public class PublishCustomNotificationTool : LlmTool<PublishCustomNotificationArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private const string CharacterLayer = "Character";

        private PersistentId id;

        protected override void Awake()
        {
            base.Awake();
            id = GetComponent<PersistentId>();
        }

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

            Register<PersistentId> ids = Environment.Current.Registers.Of<PersistentId>();

            if (arguments.IncludeEveryone)
            {
                return PublishIncludeEveryone(notification, ids);
            }

            return PublishCustomIds(notification, arguments.IncludeCustomIds ?? System.Array.Empty<long>(), ids);
        }

        private string PublishIncludeEveryone(Notification notification, Register<PersistentId> ids)
        {
            int characterLayer = LayerMask.NameToLayer(CharacterLayer);
            if (characterLayer == -1)
            {
                Log.Error($"Layer {CharacterLayer} does not exist, nobody can be notified");
                return "Failed to publish: nobody can be notified";
            }

            int characters = 0;
            int published = 0;

            foreach (PersistentId character in ids.All)
            {
                if (character.gameObject.layer != characterLayer)
                {
                    continue;
                }

                characters++;

                MainNotificationRecipient recipient = character.GetComponent<MainNotificationRecipient>();
                if (recipient == null)
                {
                    continue;
                }
                if (character.Value == id.Value)
                {
                    continue;
                }

                recipient.Receive(notification);
                published++;
            }

            Log.Info($"Entity {name} published IncludeEveryone notification to {published} / {characters} characters");
            return $"Published to {published} characters";
        }

        private string PublishCustomIds(Notification notification, long[] targetIds, Register<PersistentId> ids)
        {
            var sb = new StringBuilder();
            int published = 0;

            foreach (long targetId in targetIds)
            {
                if (targetId == id.Value)
                {
                    sb.Append($"ID {targetId} is you. ");
                    continue;
                }

                PersistentId character = ids.Of(targetId);
                if (character == null)
                {
                    sb.Append($"ID {targetId} not found. ");
                    continue;
                }

                MainNotificationRecipient recipient = character.GetComponent<MainNotificationRecipient>();
                if (recipient == null)
                {
                    sb.Append($"ID {targetId} does not accept notifications. ");
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
