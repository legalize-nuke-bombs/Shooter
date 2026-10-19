using System.Linq;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class RefusiveTalker : Talker
    {
        private ConversationManager conversationManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) return;

            conversationManager = Registers.GetAll<ConversationManager>(RegisterInactivePolicy.Include).Single();
            conversationManager.Said += Refuse;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) conversationManager.Said -= Refuse;
            base.OnNetworkDespawn();
        }

        protected override bool Busy()
        {
            return false;
        }

        private void Refuse(Conversation conversation, Message message)
        {
            if (message.AuthorId == CharacterId || conversation.Partner(message.AuthorId) != CharacterId) return;

            Character author = Character.FindById(message.AuthorId, RegisterInactivePolicy.Include);
            if (author == null || !author.TryGetComponent(out Player _)) return;

            conversationManager.Say(CharacterId, message.AuthorId, "Not now.", false, true);
        }
    }
}
