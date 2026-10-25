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

        protected override void ShooterOnEnable()
        {
            lightSwitch.Switched += Play;
        }

        protected override void ShooterOnDisable()
        {
            lightSwitch.Switched -= Play;
        }

        private void Play(bool shining)
        {
            speaker.Play(sound);
        }
    }
}
