using System;
using UnityEngine;

namespace Shooter.Game
{
    public class Ambience : MonoBehaviour
    {
        [SerializeField] private Layer[] layers;

        private AudioSource[] sources;

        private void Awake()
        {
            sources = new AudioSource[layers.Length];

            for (int i = 0; i < layers.Length; i++)
                sources[i] = Voice(layers[i]);
        }

        private void Update()
        {
            Environment environment = Environment.Current;
            if (environment == null) return;

            Clock clock = environment.Clock;
            float elevation = Celestial.Elevation((float)clock.HourAngle, clock.Declination, clock.Latitude);

            for (int i = 0; i < layers.Length; i++)
            {
                if (sources[i] != null) sources[i].volume = layers[i].Loudness(elevation);
            }
        }

        private AudioSource Voice(Layer layer)
        {
            if (layer.Loop == null) return null;

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = layer.Loop;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.Play();

            return source;
        }

        [Serializable]
        private class Layer
        {
            [SerializeField] private AudioClip loop;

            [SerializeField, Range(0f, 1f)] private float volume = 0.2f;

            [SerializeField] private float above = -90f;

            [SerializeField] private float below = 90f;

            [SerializeField] private float fade = 6f;

            public AudioClip Loop => loop;

            public float Loudness(float elevation)
            {
                float rising = Mathf.InverseLerp(above, above + fade, elevation);
                float falling = Mathf.InverseLerp(below, below - fade, elevation);

                return volume * Mathf.SmoothStep(0f, 1f, Mathf.Min(rising, falling));
            }
        }
    }
}
