using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class StaminaOverlay : Overlay
    {
        private const string BarElement = "stamina";
        private const string FillElement = "stamina-fill";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private VisualElement bar;
        private Stamina stamina;
        private VisualElement fill;
        private int shown = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Stamina own = Own();

            if (own == null)
            {
                Hide();
                return;
            }

            int percent = Mathf.RoundToInt(100f * own.Amount / own.MaxAmount);
            if (percent == shown) return;

            shown = percent;
            bar.style.display = DisplayStyle.Flex;
            fill.style.width = new Length(percent, LengthUnit.Percent);
        }

        protected override bool Bind(VisualElement root)
        {
            bar = root.Q<VisualElement>(BarElement);
            fill = root.Q<VisualElement>(FillElement);
            shown = Hidden;

            if (bar == null || fill == null)
            {
                Log.Error($"Overlay document has no {BarElement} or {FillElement} element, stamina stays hidden");
                return false;
            }

            bar.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            bar = null;
            fill = null;
        }

        private void Hide()
        {
            if (shown == Hidden) return;

            shown = Hidden;
            bar.style.display = DisplayStyle.None;
        }

        private Stamina Own()
        {
            if (stamina == null) stamina = OwnPlayer.FindComponent<Stamina>();

            return stamina;
        }
    }
}
