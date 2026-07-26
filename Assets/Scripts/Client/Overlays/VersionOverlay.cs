using UnityEngine;
using Shooter.Game;

namespace Shooter.Client.Overlays
{
    public class VersionOverlay : MonoBehaviour
    {
        private const int Margin = 10;
        private const int LineHeight = 20;
        private const int Width = 600;

        private GUIStyle style;

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } };

            GUI.Label(new Rect(Margin, Margin, Width, LineHeight), "Клиент " + Application.version, style);
            GUI.Label(new Rect(Margin, Margin + LineHeight, Width, LineHeight), Server(), style);
        }

        private static string Server()
        {
            Environment environment = Environment.Current;
            if (environment == null) return "Сервер — не подключён";

            string world = string.IsNullOrEmpty(environment.World) ? "мир без имени" : environment.World;
            return "Сервер " + environment.Version + " — " + world;
        }
    }
}
