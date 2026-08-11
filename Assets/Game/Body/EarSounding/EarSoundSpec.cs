using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Ear Sound", fileName = "EarSound")]
    public class EarSoundSpec : Spec
    {
        [SerializeField] private AudioClip[] clips;

        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0f;

        [SerializeField] private string promptDescription;
        public string PromptDescription => promptDescription;

        public float Volume => volume;

        public float PitchVariation => pitchVariation;

        public bool Silent => clips == null || clips.Length == 0;

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
