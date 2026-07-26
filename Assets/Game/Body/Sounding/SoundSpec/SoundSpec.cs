using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    [CreateAssetMenu(menuName = "Shooter/Sound", fileName = "Sound")]
    public class SoundSpec : Spec
    {
        [SerializeField] private AudioClip[] clips;

        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public float Volume => volume;

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
