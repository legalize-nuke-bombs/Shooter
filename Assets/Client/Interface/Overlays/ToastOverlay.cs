using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class ToastOverlay : Overlay
    {
        private const string FeedElement = "notifications";
        private const long Life = 5000;
        private const int Limit = 4;
        private static readonly Journal Log = Logs.Here();

        private VisualElement feed;
        private Player player;
        private IToastSource[] sources;

        private void Update()
        {
            if (!Bound) return;

            Follow();
        }

        private void Follow()
        {
            Player own = OwnPlayer.Find<Player>();
            if (own == player) return;

            Forget();
            player = own;

            if (player == null) return;

            sources = player.GetComponents<IToastSource>();
            foreach (IToastSource source in sources) source.Toasted += Show;

            Log.Info($"Toasts follow {sources.Length} sources on {player.name}");
        }

        protected override bool Bind(VisualElement root)
        {
            feed = root.Q<VisualElement>(FeedElement);

            if (feed == null)
            {
                Log.Error($"Overlay document has no {FeedElement} element, toasts stay hidden");
                return false;
            }

            feed.Clear();

            return true;
        }

        protected override void Unbind()
        {
            Forget();
            feed = null;
        }

        private void Show(Toast toast)
        {
            if (!Bound || string.IsNullOrEmpty(toast.Title)) return;

            VisualElement element = Line(toast);
            feed.Add(element);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(element.RemoveFromHierarchy).StartingIn(Life);

            Ring(toast.Sound);
        }

        private static VisualElement Line(Toast toast)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            Sprite image = toast.Icon == null ? null : toast.Icon.Sprite;

            if (image != null)
            {
                var box = new VisualElement();
                box.AddToClassList("notification__icon");
                box.style.backgroundImage = Background.FromSprite(image);
                line.Add(box);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(toast.Title);
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            if (!string.IsNullOrEmpty(toast.Subtitle))
            {
                var from = new Label(toast.Subtitle);
                from.AddToClassList("notification__from");
                body.Add(from);
            }

            line.Add(body);

            return line;
        }

        private static void Ring(EarSoundSpec sound)
        {
            if (sound == null) return;

            EarSpeaker ear = OwnPlayer.Find<EarSpeaker>();
            if (ear == null) return;

            ear.PlayLocal(sound);
        }

        private void Forget()
        {
            if (sources != null)
                foreach (IToastSource source in sources)
                    source.Toasted -= Show;

            sources = null;
            player = null;
        }
    }
}
