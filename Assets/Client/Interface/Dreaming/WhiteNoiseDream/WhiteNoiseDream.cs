using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Dreaming
{
    public sealed class WhiteNoiseDream : Dream
    {
        private readonly Texture2D noise;
        private readonly Color32[] pixels;
        private readonly float betweenFrames;

        private float untilFrame;

        public WhiteNoiseDream(VisualElement screen, WhiteNoiseDreamSpec spec) : base(screen)
        {
            int resolution = spec.Resolution;

            noise = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            pixels = new Color32[resolution * resolution];
            betweenFrames = 1f / spec.FramesPerSecond;

            Screen.style.backgroundColor = Color.black;
            Screen.style.backgroundImage = new StyleBackground(noise);
            Screen.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            Shake();
        }

        public override void Step(float dt)
        {
            untilFrame -= dt;
            if (untilFrame > 0f) return;

            untilFrame = betweenFrames;
            Shake();
        }

        public override void End()
        {
            base.End();

            Object.Destroy(noise);
        }

        private void Shake()
        {
            for (int index = 0; index < pixels.Length; index++)
            {
                var value = (byte)Random.Range(0, 256);

                pixels[index] = new Color32(value, value, value, 255);
            }

            noise.SetPixels32(pixels);
            noise.Apply(false);
        }
    }
}
