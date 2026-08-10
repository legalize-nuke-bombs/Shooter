using Shooter.Game.Body;
using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Notifying
{
    [CreateAssetMenu(menuName = "Shooter/Notification", fileName = "Notification")]
    public class NotificationSpec : Spec
    {
        [SerializeField] private IconSpec icon;
        [SerializeField] private EarSoundSpec sound;
        [SerializeField, TextArea] private string title;
        [SerializeField, TextArea] private string subtitle;
        [SerializeField, TextArea] private string told;

        public IconSpec Icon => icon;

        public EarSoundSpec Sound => sound;

        public string Title => title;

        public string Subtitle => subtitle;

        public string Told => told;

        public Notification Notify()
        {
            return new Notification(Id);
        }
    }
}
