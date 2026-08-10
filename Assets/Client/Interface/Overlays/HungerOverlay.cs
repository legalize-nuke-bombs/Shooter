using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Icons;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class HungerOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string BoxElement = "hunger";
        private const string GlyphElement = "hunger-glyph";
        private const string AmountElement = "hunger-amount";
        private const int Hidden = -1;

        [SerializeField] private IconSpec glyph;

        private VisualElement box;
        private Label amount;
        private Hunger hunger;
        private int shown = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Hunger own = Own();

            if (own == null)
            {
                Hide();
                return;
            }

            int left = Mathf.RoundToInt(own.Amount);
            if (left == shown) return;

            shown = left;
            box.style.display = DisplayStyle.Flex;
            amount.text = left.ToString();
        }

        protected override bool Bind(VisualElement root)
        {
            box = root.Q<VisualElement>(BoxElement);
            amount = root.Q<Label>(AmountElement);
            shown = Hidden;

            if (box == null || amount == null)
            {
                Log.Error($"Overlay document has no {BoxElement} box, the hunger counter stays hidden");
                return false;
            }

            VisualElement image = root.Q<VisualElement>(GlyphElement);

            if (image != null && glyph != null && glyph.Sprite != null)
                image.style.backgroundImage = Background.FromSprite(glyph.Sprite);

            box.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            box = null;
            amount = null;
        }

        private void Hide()
        {
            if (shown == Hidden) return;

            shown = Hidden;
            box.style.display = DisplayStyle.None;
        }

        private Hunger Own()
        {
            if (hunger == null) hunger = OwnPlayer.Find<Hunger>();

            return hunger;
        }
    }
}
