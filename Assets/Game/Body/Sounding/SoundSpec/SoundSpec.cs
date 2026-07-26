using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    [CreateAssetMenu(menuName = "Shooter/Sound", fileName = "Sound")]
    public class SoundSpec : Spec
    {
        [SerializeField] private AudioClip clip;

        public AudioClip Clip => clip;
    }
}
