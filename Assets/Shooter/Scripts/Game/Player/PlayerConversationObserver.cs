using System;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(PlayerConversations))]
    [RequireComponent(typeof(LocalPlayer))]
    public class PlayerConversationObserver : MonoBehaviour, IToastSource
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private IconAsset icon;
        [SerializeField] private EarSoundAsset sound;

        private readonly NameMapper nameMapper = new();
        private PlayerConversations playerConversations;
        private LocalPlayer localPlayer;

        public event Action<Toast> ToastRaised;

        private void Awake()
        {
            playerConversations = GetComponent<PlayerConversations>();
            localPlayer = GetComponent<LocalPlayer>();
        }

        private void OnEnable()
        {
            playerConversations.LiveLineArrived += HandleLiveLineArrived;
        }

        private void OnDisable()
        {
            playerConversations.LiveLineArrived -= HandleLiveLineArrived;
        }

        private void HandleLiveLineArrived(long partnerId, ConversationLine line)
        {
            if (line.Spoken || line.AuthorId == playerConversations.CharacterId) return;
            if (localPlayer.TalkPartnerId == partnerId) return;

            string partnerName = nameMapper.GetShownNameById(partnerId);
            ToastRaised?.Invoke(new Toast(icon, sound, line.Content, "от " + (string.IsNullOrEmpty(partnerName) ? Stranger : partnerName)));
        }
    }
}
