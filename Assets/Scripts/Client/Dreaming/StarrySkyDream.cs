using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Dreaming
{
    public sealed class StarrySkyDream : Dream
    {
        private readonly StarrySkyDreamSpec spec;
        private readonly VisualElement[] stars;
        private readonly float[] speeds;
        private readonly float[] phases;

        private float lived;

        public StarrySkyDream(VisualElement screen, StarrySkyDreamSpec spec) : base(screen)
        {
            this.spec = spec;

            stars = new VisualElement[spec.Stars];
            speeds = new float[spec.Stars];
            phases = new float[spec.Stars];

            Screen.style.backgroundColor = spec.Sky;

            for (int index = 0; index < stars.Length; index++)
            {
                stars[index] = Star();
                speeds[index] = Random.Range(spec.SlowestTwinkle, spec.FastestTwinkle);
                phases[index] = Random.Range(0f, Mathf.PI * 2f);

                Screen.Add(stars[index]);
            }
        }

        public override void Step(float dt)
        {
            lived += dt;

            for (int index = 0; index < stars.Length; index++)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(lived * speeds[index] + phases[index]);

                stars[index].style.opacity = Mathf.Lerp(spec.Dimmest, 1f, wave);
            }
        }

        private VisualElement Star()
        {
            float size = Random.Range(spec.SmallestStar, spec.LargestStar);

            var star = new VisualElement();
            star.style.position = Position.Absolute;
            star.style.left = new Length(Random.value * 100f, LengthUnit.Percent);
            star.style.top = new Length(Random.value * 100f, LengthUnit.Percent);
            star.style.width = size;
            star.style.height = size;
            star.style.backgroundColor = Color.white;

            return star;
        }
    }
}
