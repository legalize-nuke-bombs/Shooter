using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class AnxiousDream : Dream
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int WhisperCount = 10;
        private const int WhisperLength = 8;
        private const int TearCount = 5;
        private const float FlashChance = 0.06f;

        private static readonly Color VeilColor = new Color(0.01f, 0.008f, 0.012f, 0.97f);
        private static readonly Color TearColor = new Color(0.72f, 0.74f, 0.8f);
        private static readonly Color FlashColor = new Color(0.8f, 0.82f, 0.88f);

        private readonly Label[] whispers = new Label[WhisperCount];
        private readonly char[] noise = new char[WhisperLength];
        private bool flashing;

        public override float Weight => 1f;

        public AnxiousDream(Font font)
        {
            for (int i = 0; i < WhisperCount; i++)
            {
                var whisper = new Label();
                whisper.pickingMode = PickingMode.Ignore;
                whisper.style.position = Position.Absolute;
                whisper.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
                whispers[i] = whisper;
                Add(whisper);
            }

            generateVisualContent += OnGenerate;
            schedule.Execute(Twitch).Every(90);
        }

        private void Twitch()
        {
            foreach (Label whisper in whispers)
            {
                for (int i = 0; i < WhisperLength; i++)
                    noise[i] = Alphabet[Random.Range(0, Alphabet.Length)];
                whisper.text = new string(noise);
                whisper.style.left = Length.Percent(Random.Range(3f, 82f));
                whisper.style.top = Length.Percent(Random.Range(3f, 92f));
                whisper.style.fontSize = Random.Range(11, 26);
                whisper.style.color = new Color(0.62f, 0.64f, 0.7f, Random.Range(0.15f, 0.7f));
            }
            flashing = Random.value < FlashChance;
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
                float height = Random.Range(1f, 6f);
                float alpha = Random.Range(0.06f, 0.25f);
                painter.fillColor = new Color(TearColor.r, TearColor.g, TearColor.b, alpha);
                FillRect(painter, new Rect(0f, y, rect.width, height));
            }

            if (flashing)
            {
                painter.fillColor = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0.12f);
                FillRect(painter, new Rect(0f, 0f, rect.width, rect.height));
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
