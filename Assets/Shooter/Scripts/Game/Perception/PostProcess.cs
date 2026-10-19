using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    public sealed class PostProcess : PerceptionEffect
    {
        private readonly GameObject holder;
        private readonly PostProcessAsset asset;
        private readonly Volume volume;

        public PostProcess(PostProcessAsset asset)
        {
            this.asset = asset;

            holder = new GameObject($"Perception {asset.name}");
            volume = holder.AddComponent<Volume>();
            volume.priority = 100;
            volume.weight = 0f;
            volume.sharedProfile = asset.Profile;
        }

        public override void Tick(float strength)
        {
            volume.weight = Mathf.MoveTowards(volume.weight, strength, Time.deltaTime * asset.SettleSpeed);
        }

        public override void End()
        {
            Object.Destroy(holder);
        }
    }
}
