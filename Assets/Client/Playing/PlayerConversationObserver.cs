using Shooter.Client.Interface;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using UnityEngine;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(PlayerConversations))]
    [RequireComponent(typeof(LocalPlayer))]
    public class PlayerConversationObserver : MonoBehaviour
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private IconSpec icon;
        [SerializeField] private EarSoundSpec sound;

        private readonly NameMapper mapper = new();
        private PlayerConversations conversations;
        private LocalPlayer player;

        private void Awake()
        {
            conversations = GetComponent<PlayerConversations>();
            player = GetComponent<LocalPlayer>();
        }

        private void OnEnable()
        {
            conversations.Heard += Heard;
        }

        private void OnDisable()
        {
            conversations.Heard -= Heard;
        }

        private void Heard(long partnerId, Line line)
        {
            if (line.Spoken || line.AuthorId == conversations.CharacterId) return;
            if (player.TalkPartner == partnerId) return;

            NotificationOverlay feed = NotificationOverlay.Current;
            if (feed == null) return;

            string named = mapper.Of(partnerId);
            feed.Show(new Toast(icon, sound, line.Content, "от " + (string.IsNullOrEmpty(named) ? Stranger : named)));
        }
    }
}
