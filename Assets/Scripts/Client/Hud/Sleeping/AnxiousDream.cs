using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class AnxiousDream : Dream
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int WhisperCount = 6;
        private const int WhisperLength = 8;
        private const int TearCount = 4;

        private static readonly Color VeilColor = new Color(0.01f, 0.008f, 0.012f, 0.93f);
        private static readonly Color TearColor = new Color(0.72f, 0.74f, 0.8f);

        private readonly Label[] whispers = new Label[WhisperCount];
        private readonly char[] noise = new char[WhisperLength];

        public override float Weight => 1f;

        public AnxiousDream(Font font)
        {

            for (int i = 0; i < WhisperCount; i++)
            {
                var whisper = new Label();
                whisper.pickingMode = PickingMode.Ignore;
                whisper.style.position = Position.Absolute;
                whisper.style.fontSize = 13;
                whisper.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
                whispers[i] = whisper;
                Add(whisper);
            }

            generateVisualContent += OnGenerate;
            schedule.Execute(Twitch).Every(120);
        }

        private void Twitch()
        {
            foreach (Label whisper in whispers)
            {
                for (int i = 0; i < WhisperLength; i++)
                    noise[i] = Alphabet[Random.Range(0, Alphabet.Length)];
                whisper.text = new string(noise);
                whisper.style.left = Length.Percent(Random.Range(5f, 85f));
                whisper.style.top = Length.Percent(Random.Range(5f, 90f));
                whisper.style.color = new Color(0.6f, 0.62f, 0.68f, Random.Range(0.04f, 0.3f));
            }
            MarkDirtyRepaint();
        }

        private void OnGenerate(MeshGenerationContext mgc)
        {
            Rect rect = mgc.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            var painter = mgc.painter2D;
            painter.fillColor = VeilColor;
            FillRect(painter, new Rect(0f, 0f, rect.width, rect.height));

            for (int i = 0; i < TearCount; i++)
            {
                float y = Random.Range(0f, rect.height);
                float height = Random.Range(1f, 3f);
                float alpha = Random.Range(0.015f, 0.07f);
                painter.fillColor = new Color(TearColor.r, TearColor.g, TearColor.b, alpha);
                FillRect(painter, new Rect(0f, y, rect.width, height));
            }
        }

        private static void FillRect(Painter2D painter, Rect r)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(r.x, r.y));
            painter.LineTo(new Vector2(r.xMax, r.y));
            painter.LineTo(new Vector2(r.xMax, r.yMax));
            painter.LineTo(new Vector2(r.x, r.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
