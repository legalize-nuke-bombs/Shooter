using Shooter.Client.Playing;
using Shooter.Game.Body;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Perception
{
    public sealed class PostProcess : PerceptionEffect
    {
        private readonly PostProcessSpec spec;
        private readonly Volume volume;

        public PostProcess(PostProcessSpec spec)
        {
            this.spec = spec;

            volume = OwnPlayer.Find<IntoxicationView>().gameObject.AddComponent<Volume>();
            volume.priority = 100;
            volume.weight = 0f;
            volume.sharedProfile = spec.Profile;
        }

        public override void Tick(float strength)
        {
            volume.weight = Mathf.MoveTowards(volume.weight, strength, Time.deltaTime * spec.SettleSpeed);
        }

        public override void End()
        {
            Object.Destroy(volume);
        }
    }
}
