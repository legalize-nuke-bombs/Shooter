using System;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerConversations))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerConversationObserver : MonoBehaviour, IToastSource
    {
        [SerializeField] private IconAsset icon;
        [SerializeField] private EarSoundAsset sound;

        private PlayerConversations playerConversations;
        private PlayerController playerController;

        public event Action<Toast> ToastRaised;

        private void Awake()
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

            ToastRaised?.Invoke(new Toast(icon, sound, line.Content, "от " + NameMapper.GetShownNameById(partnerId)));
        }
    }
}
