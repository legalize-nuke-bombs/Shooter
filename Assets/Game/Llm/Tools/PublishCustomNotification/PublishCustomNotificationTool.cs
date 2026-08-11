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
`FirstLine` and `SecondLine` is the text of your notification. Write in English.

If you want everyone (both residents and strangers) to receive your notification, use the `IncludeEveryone` flag.
If you want all wanderers to receive your notification, use the `IncludeEveryWanderer` flag.
If you want the notification to be received only by specific character(s), pass their IDs to `IncludeCustomIds`.
";

        protected override string Execute(PublishCustomNotificationArguments arguments)
        {
            Log.Warn($"Entity {name} published notification {arguments.IconName} {arguments.EarSoundName} {arguments.FirstLine} {arguments.SecondLine} {arguments.IncludeEveryone} {arguments.IncludeEveryWanderer} {arguments.IncludeCustomIds.Length}");
            return "Published";
        }
    }
}
