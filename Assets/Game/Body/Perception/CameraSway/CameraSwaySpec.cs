using UnityEngine;

namespace Shooter.Game.Body.Perception
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Camera Sway", fileName = "CameraSway")]
    public class CameraSwaySpec : PerceptionEffectSpec
    {
        [SerializeField] private float swayDegrees = 4f;
        [SerializeField] private float swayFrequency = 0.4f;
        [SerializeField] private float settleSpeed = 0.3f;

        public float SwayDegrees => swayDegrees;

        public float SwayFrequency => swayFrequency;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffect Create(IPerceiver perceiver)
        {
            return new CameraSway(this, perceiver);
        }
    }
}
