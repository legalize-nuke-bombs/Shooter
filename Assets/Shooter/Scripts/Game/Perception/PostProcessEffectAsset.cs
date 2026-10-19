using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Post Process", fileName = "PostProcess")]
    public class PostProcessEffectAsset : PerceptionEffectAsset
    {
        [SerializeField] private VolumeProfile profile;
        [SerializeField] private float settleSpeed = 0.3f;

        public VolumeProfile Profile => profile;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffect CreateEffect()
        {
            return new PostProcessEffect(this);
        }
    }
}
