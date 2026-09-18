using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Audio;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Ear Sound", fileName = "EarSound")]
    public class EarSoundSpec : Spec
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private AudioClip[] clips;

        [SerializeField] private AudioMixerGroup output;

        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        [SerializeField] [Range(0f, 0.5f)] private float pitchVariation;

        [SerializeField] private string promptDescription;
        public string PromptDescription => promptDescription;

        public AudioMixerGroup Output => output;

        public float Volume => volume;

        public float PitchVariation => pitchVariation;

        public bool Silent => clips == null || clips.Length == 0;

        private void OnEnable()
        {
            if (output == null) Log.Error($"Ear sound {name} has no mixer group set, it will not follow the volume settings");
        }

        public byte Pick()
        {
            return Silent ? (byte)0 : (byte)Random.Range(0, clips.Length);
        }

        public AudioClip Clip(byte variant)
        {
            return Silent ? null : clips[variant % clips.Length];
        }
    }
}
