using UnityEngine;

namespace Shooter.Client.Interface
{
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusic : MonoBehaviour
    {
        [SerializeField] private float volume = 0.35f;
        [SerializeField] private float fadeInSeconds = 4f;

        private float elapsed;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void Start()
        {
            if (source.clip == null) return;

            source.Play();
        }

        private void Update()
        {
            if (!source.isPlaying || source.volume >= volume) return;

            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, volume, fadeInSeconds <= 0f ? 1f : elapsed / fadeInSeconds);
        }
    }
}
