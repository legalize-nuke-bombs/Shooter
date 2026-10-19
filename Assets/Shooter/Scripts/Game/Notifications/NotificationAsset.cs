using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Notification", fileName = "Notification")]
    public class NotificationAsset : Asset
    {
        [SerializeField] private IconAsset icon;
        [SerializeField] private EarSoundAsset sound;
        [SerializeField] private bool urgent;
        [SerializeField] [TextArea] private string title;
        [SerializeField] [TextArea] private string subtitle;
        [SerializeField] [TextArea] private string told;

        public IconAsset Icon => icon;

        public EarSoundAsset Sound => sound;

        public bool Urgent => urgent;

        public string Title => title;

        public string Subtitle => subtitle;

        public string Told => told;
    }
}
