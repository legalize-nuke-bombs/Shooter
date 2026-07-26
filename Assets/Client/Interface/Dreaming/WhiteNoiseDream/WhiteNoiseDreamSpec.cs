using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Dreaming
{
    [CreateAssetMenu(menuName = "Shooter/Dreams/White Noise", fileName = "WhiteNoise")]
    public sealed class WhiteNoiseDreamSpec : DreamSpec
    {
        [SerializeField] private int resolution = 128;

        [SerializeField] private float framesPerSecond = 20f;

        public int Resolution => Mathf.Clamp(resolution, 8, 512);

        public float FramesPerSecond => Mathf.Max(framesPerSecond, 1f);

        public override Dream Begin(VisualElement screen)
        {
            return new WhiteNoiseDream(screen, this);
        }
    }
}
