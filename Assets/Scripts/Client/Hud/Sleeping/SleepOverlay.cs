using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepOverlay : UiElement
    {
        private readonly SleepSense sleepSense;
        private readonly CalmDream waiting = new CalmDream();
        private readonly Dream[] dreams;
        private Dream tonight;

        public SleepOverlay(SleepSense sleepSense)
        {
            this.sleepSense = sleepSense;
            dreams = new Dream[] { waiting, new AnxiousDream() };

            Fullscreen();
            Visible = false;

            foreach (Dream dream in dreams)
                Add(dream);
        }

        protected override void OnTick(float dt)
        {
            if (!sleepSense.MySleeping)
            {
                Visible = false;
                tonight = null;
                return;
            }

            Visible = true;

            if (!sleepSense.WorldAsleep)
            {
                tonight = null;
                Show(waiting);
                return;
            }

            if (tonight == null) tonight = Pick();
            Show(tonight);
        }

        private Dream Pick()
        {
            float total = 0f;
            foreach (Dream dream in dreams)
                total += dream.Weight;

            float roll = Random.value * total;
            foreach (Dream dream in dreams)
            {
                roll -= dream.Weight;
                if (roll <= 0f) return dream;
            }
            return waiting;
        }

        private void Show(Dream active)
        {
            foreach (Dream dream in dreams)
                dream.style.display = dream == active ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
