using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class StaminaOverlay : Overlay
    {
        private const string BarElementName = "stamina";
        private const string FillElementName = "stamina-fill";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private VisualElement barElement;
        private Stamina stamina;
        private VisualElement fillElement;
        private int shownPercent = Hidden;

        private void Update()
        {
            if (!IsBound) return;

            Stamina ownStamina = FindOwnStamina();

            if (ownStamina == null)
            {
                HideBar();
                return;
            }

            int percent = Mathf.RoundToInt(100f * ownStamina.Amount / ownStamina.MaxAmount);
            if (percent == shownPercent) return;

            shownPercent = percent;
            barElement.style.display = DisplayStyle.Flex;
            fillElement.style.width = new Length(percent, LengthUnit.Percent);
        }

        protected override bool TryBindElements(VisualElement root)
        {
            barElement = root.Q<VisualElement>(BarElementName);
            fillElement = root.Q<VisualElement>(FillElementName);
            shownPercent = Hidden;

            if (barElement == null || fillElement == null)
            {
                Log.Error($"Overlay document has no {BarElementName} or {FillElementName} element, stamina stays hidden");
                return false;
            }

            barElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            barElement = null;
            fillElement = null;
        }

        private void HideBar()
        {
            if (shownPercent == Hidden) return;

            shownPercent = Hidden;
            barElement.style.display = DisplayStyle.None;
        }

        private Stamina FindOwnStamina()
        {
            if (stamina == null) stamina = OwnPlayer.FindComponent<Stamina>();

            return stamina;
        }
    }
}
