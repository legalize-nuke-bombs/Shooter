using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game.Body.Perception
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Post Process", fileName = "PostProcess")]
    public class PostProcessSpec : PerceptionEffectSpec
    {
        [SerializeField] private VolumeProfile profile;
        [SerializeField] private float settleSpeed = 0.3f;

        public VolumeProfile Profile => profile;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffect Create()
        {
            return new PostProcess(this);
        }
    }
}
