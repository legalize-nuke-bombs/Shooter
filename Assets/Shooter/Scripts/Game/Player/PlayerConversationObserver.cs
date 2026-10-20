using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(PlayerConversations))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerConversationObserver : ShooterBehaviour, IToastSource
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private IconAsset icon;
        [SerializeField] private EarSoundAsset sound;

        private readonly NameMapper nameMapper = new();
        private PlayerConversations playerConversations;
        private PlayerController playerController;

        public event Action<Toast> ToastRaised;

        protected override void ShooterAwake()
        {
            playerConversations = GetComponent<PlayerConversations>();
            playerController = GetComponent<PlayerController>();
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
            if (playerController.TalkPartnerId == partnerId) return;

            string partnerName = nameMapper.GetShownNameById(partnerId);
            ToastRaised?.Invoke(new Toast(icon, sound, line.Content, "от " + (string.IsNullOrEmpty(partnerName) ? Stranger : partnerName)));
        }
    }
}
