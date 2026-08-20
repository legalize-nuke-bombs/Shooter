using System.Collections;
using Shooter.Game.Core.Saves;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(SaveManager))]
    public class SaveHotkey : MonoBehaviour
    {
        [SerializeField] private Key key = Key.F5;

        private SaveManager saveManager;
        private bool saving;

        private void Awake()
        {
            saveManager = GetComponent<SaveManager>();
        }

        private void Update()
        {
            if (saving) return;
            if (Keyboard.current == null || !Keyboard.current[key].wasPressedThisFrame) return;

            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            StartCoroutine(GuardedSaveCoroutine());
        }

        private IEnumerator GuardedSaveCoroutine()
        {
            saving = true;
            yield return saveManager.SaveCoroutine();
            saving = false;
        }
    }
}
