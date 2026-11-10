using System;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerConversations))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(EarSpeaker))]
    public class PlayerConversationObserver : MonoBehaviour, IToastSource
    {
        [SerializeField] private IconAsset icon;
        [SerializeField] private EarSoundAsset sound;

        private PlayerConversations playerConversations;
        private PlayerController playerController;
        private EarSpeaker earSpeaker;

        public event Action<Toast> ToastRaised;

        private void Awake()
        {
            playerConversations = GetComponent<PlayerConversations>();
            playerController = GetComponent<PlayerController>();
            earSpeaker = GetComponent<EarSpeaker>();
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

            if (playerController.TalkPartnerId == partnerId)
            {
                earSpeaker.PlayLocally(sound);
                return;
            }

            ToastRaised?.Invoke(new Toast(icon, sound, line.Content, "от " + NameMapper.GetShownNameById(partnerId)));
        }
    }
}
