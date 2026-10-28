using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class ScreenshotOnKey : ShooterComponent, IShooterUpdateable
    {
        private const string FolderName = "Screenshots";
        private const string FileNamePrefix = "Screenshot";
        private const string StampFormat = "yyyy_MM_dd_HH_mm_ss";
        private static readonly ScreenshotSetting FullSizeSetting = new(0, 0, 90);

        private AudioSource click;

        protected override void ShooterAwake()
        {
            click = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }

        public void ShooterUpdate()
        {
            if (Keyboard.current == null || !Keyboard.current.f12Key.wasPressedThisFrame) return;

            TakeScreenshot();
        }

        private void TakeScreenshot()
        {
            string stamp = DateTime.Now.ToString(StampFormat, CultureInfo.InvariantCulture);
            string path = Path.Combine(Config.GetRootFolder(), FolderName, FileNamePrefix + "_" + stamp + ".jpg");

            click.Play();
            StartCoroutine(Screenshotter.TakeScreenshot(path, FullSizeSetting));
        }
    }
}
