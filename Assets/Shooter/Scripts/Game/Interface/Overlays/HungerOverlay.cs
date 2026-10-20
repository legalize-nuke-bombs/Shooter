using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class HungerOverlay : Overlay
    {
        private const string CounterElementName = "hunger";
        private const string GlyphElementName = "hunger-glyph";
        private const string AmountLabelName = "hunger-amount";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private IconAsset glyph;
        private Label amountLabel;

        private VisualElement counterElement;
        private Hunger hunger;
        private int shownAmount = Hidden;

        private void Update()
        {
            if (!IsBound) return;

            Hunger localHunger = FindLocalHunger();

            if (localHunger == null)
            {
                HideCounter();
                return;
            }

            int amount = Mathf.RoundToInt(localHunger.Amount);
            if (amount == shownAmount) return;

            shownAmount = amount;
            counterElement.style.display = DisplayStyle.Flex;
            amountLabel.text = amount.ToString();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            counterElement = root.Q<VisualElement>(CounterElementName);
            amountLabel = root.Q<Label>(AmountLabelName);
            shownAmount = Hidden;

            if (counterElement == null || amountLabel == null)
            {
                Log.Error($"Overlay document has no {CounterElementName} box, the hunger counter stays hidden");
                return false;
            }

            VisualElement glyphElement = root.Q<VisualElement>(GlyphElementName);

            if (glyphElement != null && glyph != null && glyph.Sprite != null)
            {
                glyphElement.style.backgroundImage = Background.FromSprite(glyph.Sprite);
            }

            counterElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            counterElement = null;
            amountLabel = null;
        }

        private void HideCounter()
        {
            if (shownAmount == Hidden) return;

            shownAmount = Hidden;
            counterElement.style.display = DisplayStyle.None;
        }

        private Hunger FindLocalHunger()
        {
            if (hunger == null) hunger = LocalPlayerFinder.FindComponent<Hunger>();

            return hunger;
        }
    }
}
