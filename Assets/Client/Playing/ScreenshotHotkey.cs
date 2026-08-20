using System;
using System.Globalization;
using System.IO;
using Shooter.Configuring;
using Shooter.Game.Core.Screenshots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shooter.Client.Playing
{
    public class ScreenshotHotkey : MonoBehaviour
    {
        [SerializeField] private Key key = Key.F12;
        [SerializeField] private ScreenshotSetting setting;
        [SerializeField] private string folder = "Screenshots";
        [SerializeField] private string prefix = "Screenshot";
        [SerializeField] private string stampFormat = "yyyy_MM_dd_HH_mm_ss";

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current[key].wasPressedThisFrame) return;

            string stamp = DateTime.Now.ToString(stampFormat, CultureInfo.InvariantCulture);
            string path = Path.Combine(Config.Root(), folder, prefix + "_" + stamp + ".jpg");
            StartCoroutine(ScreenshotManager.Current.WriteCoroutine(path, setting));
        }
    }
}
