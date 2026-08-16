using Shooter.Client.Playing;
using Shooter.Game.Body;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Client.Perception
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Alcohol", fileName = "AlcoholPerception")]
    public class AlcoholPerceptionEffect : PerceptionEffect
    {
        [SerializeField] private float maxDistortion = 0.35f;
        [SerializeField] private float swayDegrees = 4f;
        [SerializeField] private float swayFrequency = 0.4f;
        [SerializeField] private float settleSpeed = 0.3f;

        public float MaxDistortion => maxDistortion;

        public float SwayDegrees => swayDegrees;

        public float SwayFrequency => swayFrequency;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffectInstance Begin()
        {
            return new Drunkenness(this);
        }

        private sealed class Drunkenness : PerceptionEffectInstance
        {
            private readonly AlcoholPerceptionEffect config;
            private readonly IntoxicationView view;
            private readonly GameObject holder;
            private readonly VolumeProfile profile;
            private readonly LensDistortion distortion;
            private float felt;

            public Drunkenness(AlcoholPerceptionEffect config)
            {
                this.config = config;
                view = OwnPlayer.Find<IntoxicationView>();

                holder = new GameObject("AlcoholPerception");
                Volume volume = holder.AddComponent<Volume>();
                volume.priority = 100;

                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                distortion = profile.Add<LensDistortion>();
                distortion.intensity.overrideState = true;
                distortion.intensity.value = 0f;
                volume.sharedProfile = profile;
            }

            public override void Tick(float strength)
            {
                felt = Mathf.MoveTowards(felt, strength, Time.deltaTime * config.SettleSpeed);

                distortion.intensity.value = config.MaxDistortion * felt;

                float phase = Time.time * config.SwayFrequency * Mathf.PI * 2f;
                float roll = Mathf.Sin(phase) * config.SwayDegrees * felt;
                float nod = Mathf.Sin(phase * 0.7f) * config.SwayDegrees * 0.4f * felt;

                if (view != null) view.CameraSway = new Vector3(nod, 0f, roll);
            }

            public override void End()
            {
                if (view != null) view.CameraSway = Vector3.zero;

                Object.Destroy(holder);
                Object.Destroy(profile);
            }
        }
    }
}
