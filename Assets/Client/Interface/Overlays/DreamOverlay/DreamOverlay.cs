using Shooter.Client.Interface.Dreaming;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Overlays
{
    public class DreamOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string DreamElement = "dream";

        [SerializeField] private DreamCatalog dreams;

        private VisualElement screen;
        private Dream dream;
        private bool asleep;

        private void Update()
        {
            if (!Bound) return;

            bool everyoneAsleep = WorldAsleep();

            if (everyoneAsleep != asleep)
            {
                asleep = everyoneAsleep;

                if (everyoneAsleep) Fall();
                else Wake();
            }

            dream?.Step(Time.deltaTime);
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(DreamElement);
            asleep = false;

            if (screen == null)
            {
                Log.Error("Overlay document has no {} element, dreams stay unseen", DreamElement);
                return false;
            }

            if (dreams == null)
            {
                Log.Error("Dream overlay has no dream catalog, dreams stay unseen");
                return false;
            }

            screen.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            if (dream != null) Wake();

            screen = null;
            asleep = false;
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
