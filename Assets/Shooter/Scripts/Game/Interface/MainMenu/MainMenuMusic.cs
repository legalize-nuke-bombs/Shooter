using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuMusic : MonoBehaviour, IShooterUpdateable
    {
        [SerializeField] private AudioClip[] tracks;
        [SerializeField] private float fadeInSeconds = 4f;

        private float elapsedFadeInSeconds;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }

        private void Start()
        {
            if (tracks == null || tracks.Length == 0) return;

            audioSource.clip = tracks[Random.Range(0, tracks.Length)];
            audioSource.Play();
        }

        public void ShooterUpdate()
        {
            if (!audioSource.isPlaying || audioSource.volume >= 1f) return;

            elapsedFadeInSeconds += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, fadeInSeconds <= 0f ? 1f : elapsedFadeInSeconds / fadeInSeconds);
        }
    }
}
