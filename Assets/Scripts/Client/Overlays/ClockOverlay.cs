using UnityEngine;
using Shooter.Game;

namespace Shooter.Client.Overlays
{
    public class ClockOverlay : MonoBehaviour
    {
        private const int Margin = 10;
        private const int LineHeight = 20;
        private const int Width = 240;

        private GUIStyle style;

        private void OnGUI()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            style ??= new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperRight
            };

            var at = new Rect(Screen.width - Width - Margin, Margin, Width, LineHeight);
            GUI.Label(at, environment.Clock.DateTime(), style);
        }
    }
}
