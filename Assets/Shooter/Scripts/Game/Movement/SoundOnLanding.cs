using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(RpcMovement))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(CharacterController))]
    public class SoundOnLanding : MonoBehaviour
    {
        [SerializeField] private float minHeight = 0.6f;
        [SerializeField] private SurfaceSounds sounds;
        [SerializeField] private float gain = 1.7f;

        private RpcMovement movement;
        private Speaker speaker;
        private CharacterController characterController;

        private void Awake()
        {
            movement = GetComponent<RpcMovement>();
            speaker = GetComponent<Speaker>();
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            movement.Landed += Play;
        }

        private void OnDisable()
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
