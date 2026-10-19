using System.Linq;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class RefusiveTalker : Talker
    {
        private ConversationManager conversations;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) return;

            conversations = Registers.Of<ConversationManager>(RegisterInactivePolicy.Include).Single();
            conversations.Said += Refuse;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) conversations.Said -= Refuse;
            base.OnNetworkDespawn();
        }

        protected override bool Busy()
        {
            return false;
        }

        private void Refuse(Conversation conversation, Message message)
        {
            if (message.AuthorId == CharacterId || conversation.Partner(message.AuthorId) != CharacterId) return;

            Character author = Character.Of(message.AuthorId, RegisterInactivePolicy.Include);
            if (author == null || !author.TryGetComponent(out Player _)) return;

            conversations.Say(CharacterId, message.AuthorId, "Not now.", false, true);
        }
    }
}
