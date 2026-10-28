using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnFallAsleep : MonoBehaviour
    {
        [SerializeField] private SoundAsset sound;

        private Sleeper sleeper;
        private Speaker speaker;

        private void Awake()
        {
            sleeper = GetComponent<Sleeper>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            sleeper.FellAsleep += Play;
        }

        private void OnDisable()
        {
            sleeper.FellAsleep -= Play;
        }

        private void Play()
        {
            speaker.Play(sound);
        }
    }
}
