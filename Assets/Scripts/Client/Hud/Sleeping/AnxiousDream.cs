using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class AnxiousDream : Dream
    {
        private const int NoiseWidth = 192;
        private const int NoiseHeight = 108;

        private readonly Texture2D noise;
        private readonly Color32[] pixels = new Color32[NoiseWidth * NoiseHeight];

        public override float Weight => 1f;

        public AnxiousDream()
        {
            noise = new Texture2D(NoiseWidth, NoiseHeight, TextureFormat.RGBA32, false);
            noise.filterMode = FilterMode.Point;
            style.backgroundImage = new StyleBackground(noise);
            schedule.Execute(Regenerate).Every(50);
        }

        private void Regenerate()
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                byte shade = (byte)Random.Range(0, 256);
                pixels[i] = new Color32(shade, shade, shade, 255);
            }
            noise.SetPixels32(pixels);
            noise.Apply();
        }
    }
}
