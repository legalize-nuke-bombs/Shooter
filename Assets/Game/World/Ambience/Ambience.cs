using System.Collections.Generic;
using Shooter.Game.Core.Mixing;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(AudioSource))]
    public class Ambience : MonoBehaviour
    {
        [SerializeField] private AudioClip[] tracks;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.2f;

        private readonly List<int> bag = new();
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = Mixer.Ambience;
            source.volume = volume;
        }

        private void Update()
        {
            if (source.isPlaying || tracks == null || tracks.Length == 0) return;

            source.clip = tracks[Next()];
            source.Play();
        }

        private int Next()
        {
            if (bag.Count == 0) Refill();

            int last = bag.Count - 1;
            int index = bag[last];
            bag.RemoveAt(last);
            return index;
        }

        private void Refill()
        {
            for (int i = 0; i < tracks.Length; i++) bag.Add(i);

            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }

            // не открывать новый круг тем же треком, что звучал последним
            if (source.clip != null && tracks.Length > 1 && tracks[bag[^1]] == source.clip)
                (bag[^1], bag[0]) = (bag[0], bag[^1]);
        }
    }
}
