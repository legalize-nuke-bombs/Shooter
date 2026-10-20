using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ToastOverlay : Overlay
    {
        private const string FeedElementName = "toasts";
        private const long ToastLifetimeMilliseconds = 5000;
        private const int MaxShownToasts = 4;
        private static readonly Journal Log = Logs.Here();

        private VisualElement feedElement;
        private Player subscribedPlayer;
        private IToastSource[] toastSources;

        private void Update()
        {
            if (!IsBound) return;

            SubscribeToLocalPlayerToastSourcesIfChanged();
        }

        private void SubscribeToLocalPlayerToastSourcesIfChanged()
        {
            Player localPlayer = LocalPlayerFinder.FindComponent<Player>();
            if (localPlayer == subscribedPlayer) return;

            UnsubscribeFromToastSources();
            subscribedPlayer = localPlayer;

            if (subscribedPlayer == null) return;

            toastSources = subscribedPlayer.GetComponents<IToastSource>();
            foreach (IToastSource toastSource in toastSources)
            {
                toastSource.ToastRaised += ShowToast;
            }

            Log.Info($"Toasts follow {toastSources.Length} sources on {subscribedPlayer.name}");
        }

        protected override bool TryBindElements(VisualElement root)
        {
            feedElement = root.Q<VisualElement>(FeedElementName);

            if (feedElement == null)
            {
                Log.Error($"Overlay document has no {FeedElementName} element, toasts stay hidden");
                return false;
            }

            feedElement.Clear();

            return true;
        }

        protected override void UnbindElements()
        {
            UnsubscribeFromToastSources();
            feedElement = null;
        }

        private void ShowToast(Toast toast)
        {
            if (!IsBound || string.IsNullOrEmpty(toast.Title)) return;

            VisualElement toastElement = CreateToastElement(toast);
            feedElement.Add(toastElement);

            while (feedElement.childCount > MaxShownToasts)
            {
                feedElement.RemoveAt(0);
            }

            feedElement.schedule.Execute(toastElement.RemoveFromHierarchy).StartingIn(ToastLifetimeMilliseconds);

            PlayToastSound(toast.Sound);
        }

        private static VisualElement CreateToastElement(Toast toast)
        {
            VisualElement toastElement = new();
            toastElement.AddToClassList("toast");

            Sprite iconSprite = toast.Icon == null ? null : toast.Icon.Sprite;

            if (iconSprite != null)
            {
                VisualElement iconElement = new();
                iconElement.AddToClassList("toast__icon");
                iconElement.style.backgroundImage = Background.FromSprite(iconSprite);
                toastElement.Add(iconElement);
            }

            VisualElement bodyElement = new();
            bodyElement.AddToClassList("toast__body");

            Label titleLabel = new(toast.Title);
            titleLabel.AddToClassList("line");
            titleLabel.AddToClassList("toast__title");
            bodyElement.Add(titleLabel);

            if (!string.IsNullOrEmpty(toast.Subtitle))
            {
                Label subtitleLabel = new(toast.Subtitle);
                subtitleLabel.AddToClassList("toast__from");
                bodyElement.Add(subtitleLabel);
            }

            toastElement.Add(bodyElement);

            return toastElement;
        }

        private static void PlayToastSound(EarSoundAsset sound)
        {
            if (sound == null) return;

            EarSpeaker earSpeaker = LocalPlayerFinder.FindComponent<EarSpeaker>();
            if (earSpeaker == null) return;

            earSpeaker.PlayLocally(sound);
        }

        private void UnsubscribeFromToastSources()
        {
            if (toastSources != null)
            {
                foreach (IToastSource toastSource in toastSources)
                {
                    toastSource.ToastRaised -= ShowToast;
                }
            }

            toastSources = null;
            subscribedPlayer = null;
        }
    }
}
