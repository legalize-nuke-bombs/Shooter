using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Switch))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnSwitch : MonoBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Switch lightSwitch;
        private Speaker speaker;

        private void Awake()
        {
            lightSwitch = GetComponent<Switch>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            lightSwitch.Switched += Play;
        }

        private void OnDisable()
        {
            lightSwitch.Switched -= Play;
        }

        private void Play(bool shining)
        {
            speaker.Play(sound);
        }
    }
}
