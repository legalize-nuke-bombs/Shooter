using Shooter.Game.Body;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Perception
{
    [CreateAssetMenu(menuName = "Shooter/Perception/Volume", fileName = "Volume")]
    public class VolumeEffect : PerceptionEffect
    {
        [SerializeField] private VolumeProfile profile;
        [SerializeField] private float settleSpeed = 0.3f;

        public VolumeProfile Profile => profile;

        public float SettleSpeed => settleSpeed;

        public override PerceptionEffectInstance Begin()
        {
            return new Overriding(this);
        }

        private sealed class Overriding : PerceptionEffectInstance
        {
            private readonly VolumeEffect config;
            private readonly GameObject holder;
            private readonly Volume volume;

            public Overriding(VolumeEffect config)
            {
                this.config = config;

                holder = new GameObject($"Perception {config.name}");
                volume = holder.AddComponent<Volume>();
                volume.priority = 100;
                volume.weight = 0f;
                volume.sharedProfile = config.Profile;
            }

            public override void Tick(float strength)
            {
                volume.weight = Mathf.MoveTowards(volume.weight, strength, Time.deltaTime * config.SettleSpeed);
            }

            public override void End()
            {
                Object.Destroy(holder);
            }
        }
    }
}
