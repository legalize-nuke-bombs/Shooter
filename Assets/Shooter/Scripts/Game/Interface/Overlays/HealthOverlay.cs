using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class HealthOverlay : Overlay
    {
        private const string BarElementName = "health";
        private const string FillElementName = "health-fill";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private VisualElement barElement;
        private VisualElement fillElement;
        private Health health;
        private int shownHp = Hidden;

        private void Update()
        {
            if (!IsBound) return;

            Health ownHealth = FindOwnHealth();

            if (ownHealth == null)
            {
                HideBar();
                return;
            }

            int hp = (int)Math.Ceiling(ownHealth.Hp);
            if (hp == shownHp) return;

            shownHp = hp;
            barElement.style.display = DisplayStyle.Flex;
            fillElement.style.width = new Length(100f * hp / (int)ownHealth.MaxHp, LengthUnit.Percent);
        }

        protected override bool TryBindElements(VisualElement root)
        {
            barElement = root.Q<VisualElement>(BarElementName);
            fillElement = root.Q<VisualElement>(FillElementName);
            shownHp = Hidden;

            if (barElement == null || fillElement == null)
            {
                Log.Error($"Overlay document has no {BarElementName} or {FillElementName} element, health stays hidden");
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
            if (shownHp == Hidden) return;

            shownHp = Hidden;
            barElement.style.display = DisplayStyle.None;
        }

        private Health FindOwnHealth()
        {
            if (health == null) health = OwnPlayer.FindComponent<Health>();

            return health;
        }
    }
}
