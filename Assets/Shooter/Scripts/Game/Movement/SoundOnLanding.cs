using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RpcMovement))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(CharacterController))]
    public class SoundOnLanding : ShooterComponent
    {
        [SerializeField] private float minHeight = 0.6f;
        [SerializeField] private SurfaceSounds sounds;
        [SerializeField] private float gain = 1.7f;

        private RpcMovement movement;
        private Speaker speaker;
        private CharacterController characterController;

        protected override void ShooterAwake()
        {
            movement = GetComponent<RpcMovement>();
            speaker = GetComponent<Speaker>();
            characterController = GetComponent<CharacterController>();
        }

        protected override void ShooterOnEnable()
        {
            movement.Landed += Play;
        }

        protected override void ShooterOnDisable()
        {
            movement.Landed -= Play;
        }

        private void Play(float fallHeight)
        {
            if (fallHeight < minHeight) return;

            speaker.Play(sounds == null ? null : sounds.GetSoundOn(Surface.FindUnder(characterController)), gain);
        }
    }
}
