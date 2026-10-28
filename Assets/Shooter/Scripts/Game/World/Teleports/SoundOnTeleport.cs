using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Teleporter))]
    public class SoundOnTeleport : MonoBehaviour
    {
        [SerializeField] private EarSoundAsset sound;

        private Teleporter teleporter;

        private void Awake()
        {
            teleporter = GetComponent<Teleporter>();
        }

        private void OnEnable()
        {
            teleporter.Teleported += PlayForTeleported;
        }

        private void OnDisable()
        {
            teleporter.Teleported -= PlayForTeleported;
        }

        private void PlayForTeleported(Character character)
        {
            character.GetComponent<EarSpeaker>()?.PlayForOwner(sound);
        }
    }
}
