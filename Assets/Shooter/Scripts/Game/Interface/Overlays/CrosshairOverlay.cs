using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class CrosshairOverlay : Overlay, IShooterUpdateable
    {
        private const string CrosshairElementName = "crosshair";
        private static readonly Journal Log = Logs.Here();

        private VisualElement crosshairElement;
        private bool shown;

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            bool shouldBeShown = playerController == null || !playerController.IsAnyWindowOpen;
            if (shouldBeShown == shown) return;

            shown = shouldBeShown;
            crosshairElement.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override bool TryBindElements(VisualElement root)
        {
            crosshairElement = root.Q<VisualElement>(CrosshairElementName);
            shown = true;

            if (crosshairElement == null)
            {
                Log.Error($"Overlay document has no {CrosshairElementName} element, the crosshair stays under windows");
                return false;
            }

            return true;
        }

        protected override void UnbindElements()
        {
            crosshairElement = null;
        }
    }
}
