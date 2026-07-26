using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Logging;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public class HealthOverlay : MonoBehaviour
    {
        private const string BarElement = "health";
        private const string FillElement = "health-fill";
        private const int Hidden = -1;

        private PanelRenderer panel;
        private VisualElement bar;
        private VisualElement fill;
        private Health health;
        private int shown = Hidden;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);
            bar = null;
            fill = null;
        }

        private void Update()
        {
            if (bar == null) return;

            Health own = Own();

            if (own == null)
            {
                Hide();
                return;
            }

            int hp = own.Hp;
            if (hp == shown) return;

            shown = hp;
            bar.style.display = DisplayStyle.Flex;
            fill.style.width = new Length(100f * hp / own.MaxHp, LengthUnit.Percent);
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            bar = root.Q<VisualElement>(BarElement);
            fill = root.Q<VisualElement>(FillElement);
            shown = Hidden;

            if (bar == null || fill == null)
            {
                Log.Error("Overlay document has no {} or {} element, health stays hidden", BarElement, FillElement);
                bar = null;
                return;
            }

            bar.style.display = DisplayStyle.None;
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
