using Shooter.Game.Core.Mixing;
using UnityEngine;

namespace Shooter.Client.Interface
{
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip[] tracks;
        [SerializeField] private float fadeInSeconds = 4f;

        private float elapsed;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = Mixer.Music;
            source.volume = 0f;
        }

        private void Start()
        {
            if (tracks == null || tracks.Length == 0) return;

            source.clip = tracks[Random.Range(0, tracks.Length)];
            source.Play();
        }

        private void Update()
        {
            if (!source.isPlaying || source.volume >= 1f) return;

            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, fadeInSeconds <= 0f ? 1f : elapsed / fadeInSeconds);
        }
    }
}
