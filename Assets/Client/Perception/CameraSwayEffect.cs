using Shooter.Client.Playing;
using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Client.Perception
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Camera Sway", fileName = "CameraSway")]
    public class CameraSwayEffect : PerceptionEffect
    {
        [SerializeField] private float swayDegrees = 4f;
        [SerializeField] private float swayFrequency = 0.4f;
        [SerializeField] private float settleSpeed = 0.3f;

        public float SwayDegrees => swayDegrees;

        public float SwayFrequency => swayFrequency;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffectInstance Begin()
        {
            return new Swaying(this);
        }

        private sealed class Swaying : PerceptionEffectInstance
        {
            private const float NodShare = 0.4f;
            private const float NodFrequencyShare = 0.7f;

            private readonly CameraSwayEffect config;
            private readonly IntoxicationView view;
            private float felt;

            public Swaying(CameraSwayEffect config)
            {
                this.config = config;
                view = OwnPlayer.Find<IntoxicationView>();
            }

            public override void Tick(float strength)
            {
                felt = Mathf.MoveTowards(felt, strength, Time.deltaTime * config.SettleSpeed);

                float phase = Time.time * config.SwayFrequency * Mathf.PI * 2f;
                float roll = Mathf.Sin(phase) * config.SwayDegrees * felt;
                float nod = Mathf.Sin(phase * NodFrequencyShare) * config.SwayDegrees * NodShare * felt;

                if (view != null) view.CameraSway += new Vector3(nod, 0f, roll);
            }

            public override void End()
            {
            }
        }
    }
}
