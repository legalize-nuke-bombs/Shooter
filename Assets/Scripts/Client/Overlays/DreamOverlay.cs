using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Dreaming;
using Shooter.Logging;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public class DreamOverlay : MonoBehaviour
    {
        private const string DreamElement = "dream";

        [SerializeField] private DreamCatalog dreams;

        private PanelRenderer panel;
        private VisualElement screen;
        private Dream dream;
        private bool asleep;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);

            if (dream != null) Wake();

            screen = null;
        }

        private void Update()
        {
            if (screen == null) return;

            bool everyoneAsleep = WorldAsleep();

            if (everyoneAsleep != asleep)
            {
                asleep = everyoneAsleep;

                if (everyoneAsleep) Fall();
                else Wake();
            }

            dream?.Step(Time.deltaTime);
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            if (dream != null) Wake();

            screen = root.Q<VisualElement>(DreamElement);
            asleep = false;

            if (screen == null)
            {
                Log.Error("Overlay document has no {} element, dreams stay unseen", DreamElement);
                return;
            }

            if (dreams == null)
            {
                Log.Error("Dream overlay has no dream catalog, dreams stay unseen");
                screen = null;
                return;
            }

            screen.style.display = DisplayStyle.None;
        }

        private void Fall()
        {
            DreamSpec spec = dreams.Pick();
            if (spec == null) return;

            screen.style.display = DisplayStyle.Flex;
            dream = spec.Begin(screen);
            Log.Info("Everyone is asleep, the night is dreamt as {}", spec.name);
        }

        private void Wake()
        {
            if (dream != null)
            {
                dream.End();
                dream = null;
            }

            if (screen != null) screen.style.display = DisplayStyle.None;

            Log.Info("The world is awake, the dream is over");
        }

        private static bool WorldAsleep()
        {
            Environment environment = Environment.Current;

            return environment != null && environment.SleepCycle.WorldAsleep;
        }
    }
}
