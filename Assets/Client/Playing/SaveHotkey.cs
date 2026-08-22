using System.Collections;
using Shooter.Game.Body;
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
        [SerializeField] private EarSoundSpec sound;

        private SaveManager saveManager;

        private void Awake()
        {
            saveManager = GetComponent<SaveManager>();
        }

        private void Update()
        {
            if (saveManager.Saving) return;
            if (Keyboard.current == null || !Keyboard.current[key].wasPressedThisFrame) return;

            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            EarSpeaker speaker = OwnPlayer.Find<EarSpeaker>();
            if (speaker != null) speaker.PlayLocal(sound);

            StartCoroutine(saveManager.SaveCoroutine());
        }
    }
}
