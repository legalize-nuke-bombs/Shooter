using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    public sealed class PostProcessEffect : PerceptionEffect
    {
        private readonly PostProcessEffectAsset asset;
        private readonly GameObject volumeObject;
        private readonly Volume volume;

        public PostProcessEffect(PostProcessEffectAsset asset)
        {
            this.asset = asset;

            volumeObject = new GameObject($"Perception {asset.name}");
            volume = volumeObject.AddComponent<Volume>();
            volume.priority = 100;
            volume.weight = 0f;
            volume.sharedProfile = asset.Profile;
        }

        public override void UpdateStrength(float strength)
        {
            volume.weight = Mathf.MoveTowards(volume.weight, strength, Time.deltaTime * asset.SettleSpeed);
        }

        public override void EndEffect()
        {
            Object.Destroy(volumeObject);
        }
    }
}
