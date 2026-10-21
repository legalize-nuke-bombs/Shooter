using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Switch))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnSwitch : ShooterBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Switch lightSwitch;
        private Speaker speaker;

        protected override void ShooterAwake()
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
            speaker.PlayForEveryone(sound);
        }
    }
}
