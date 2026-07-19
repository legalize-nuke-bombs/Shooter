using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Hud.Sleeping;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepOverlay : VisualElement
    {
        private readonly SleepSense sleepSense;
        private readonly CalmDream waiting = new CalmDream();
        private readonly Dream[] dreams;
        private Dream tonight;

        public SleepOverlay(SleepSense sleepSense)
        {
            this.sleepSense = sleepSense;
            dreams = new Dream[] { waiting, new AnxiousDream() };

            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.display = DisplayStyle.None;

            foreach (Dream dream in dreams)
                Add(dream);

            schedule.Execute(Refresh).Every(16);
        }

        private void Refresh()
        {
            if (!sleepSense.MySleeping)
            {
                style.display = DisplayStyle.None;
                tonight = null;
                return;
            }

            style.display = DisplayStyle.Flex;

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
