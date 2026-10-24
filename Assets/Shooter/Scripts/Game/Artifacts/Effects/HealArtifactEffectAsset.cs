using System.Globalization;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Artifacts/Heal Artifact Effect", fileName = "HealArtifactEffect")]
    public class HealArtifactEffectAsset : ArtifactEffectAsset
    {
        [SerializeField] private float speed;

        public override ArtifactEffect Create()
        {
            return new HealArtifactEffect(speed);
        }

        public override ItemStat Stat
        {
            get
            {
                string perMinute = (speed * 60f).ToString("0.#", CultureInfo.InvariantCulture);
                return new ItemStat("Здоровье", $"+{perMinute} в минуту", $"Heal {perMinute} per minute while held");
            }
        }
    }
}
