using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(AudioSource))]
    public class Ambience : MonoBehaviour
    {
        [SerializeField] private AudioClip[] tracks;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.2f;

        private readonly List<int> shuffledTrackIndices = new();
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = volume;
        }

        private void Update()
        {
            if (audioSource.isPlaying || tracks == null || tracks.Length == 0) return;

            audioSource.clip = tracks[TakeNextTrackIndex()];
            audioSource.Play();
        }

        private int TakeNextTrackIndex()
        {
            if (shuffledTrackIndices.Count == 0) RefillShuffledTrackIndices();

            int last = shuffledTrackIndices.Count - 1;
            int trackIndex = shuffledTrackIndices[last];
            shuffledTrackIndices.RemoveAt(last);
            return trackIndex;
        }

        private void RefillShuffledTrackIndices()
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                shuffledTrackIndices.Add(i);
            }

            for (int i = shuffledTrackIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffledTrackIndices[i], shuffledTrackIndices[j]) = (shuffledTrackIndices[j], shuffledTrackIndices[i]);
            }

            // A new round must not open with the track that has just played
            if (audioSource.clip != null && tracks.Length > 1 && tracks[shuffledTrackIndices[^1]] == audioSource.clip)
            {
                (shuffledTrackIndices[^1], shuffledTrackIndices[0]) = (shuffledTrackIndices[0], shuffledTrackIndices[^1]);
            }
        }
    }
}
