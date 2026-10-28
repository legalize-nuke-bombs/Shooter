using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnWakeUp : MonoBehaviour
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
            sleeper.WokeUp += Play;
        }

        private void OnDisable()
        {
            sleeper.WokeUp -= Play;
        }

        private void Play()
        {
            speaker.Play(sound);
        }
    }
}
