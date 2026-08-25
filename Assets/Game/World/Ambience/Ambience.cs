using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(AudioSource))]
    public class Ambience : MonoBehaviour
    {
        [SerializeField] private AudioClip[] tracks;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.2f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
        }

        private void Update()
        {
            if (source.isPlaying || tracks == null || tracks.Length == 0) return;

            source.clip = tracks[Random.Range(0, tracks.Length)];
            source.Play();
        }
    }
}
