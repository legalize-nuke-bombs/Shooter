using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Shooter.Logging;

namespace Shooter.Client.Input
{
    public class ExitHotkey : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isBatchMode) enabled = false;
        }

        private void Update()
        {
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            Log.Info("Exit: Escape pressed, leaving to menu");
            SceneManager.LoadScene("Menu");
        }
    }
}
