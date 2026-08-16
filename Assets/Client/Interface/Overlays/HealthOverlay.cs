using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class HealthOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string BarElement = "health";
        private const string FillElement = "health-fill";
        private const int Hidden = -1;

        private VisualElement bar;
        private VisualElement fill;
        private Health health;
        private int shown = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Health own = Own();

            if (own == null)
            {
                Hide();
                return;
            }

            int hp = (int)System.Math.Ceiling(own.Hp);
            if (hp == shown) return;

            shown = hp;
            bar.style.display = DisplayStyle.Flex;
            fill.style.width = new Length(100f * hp / (int)own.MaxHp, LengthUnit.Percent);
        }

        protected override bool Bind(VisualElement root)
        {
            bar = root.Q<VisualElement>(BarElement);
            fill = root.Q<VisualElement>(FillElement);
            shown = Hidden;

            if (bar == null || fill == null)
            {
                Log.Error($"Overlay document has no {BarElement} or {FillElement} element, health stays hidden");
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

        private Health Own()
        {
            if (health == null) health = OwnPlayer.Find<Health>();

            return health;
        }
    }
}
