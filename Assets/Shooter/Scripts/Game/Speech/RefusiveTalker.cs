using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class RefusiveTalker : Talker
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) Map.Get<ConversationManager>().Said += Refuse;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && Map.Get<ConversationManager>() != null) Map.Get<ConversationManager>().Said -= Refuse;
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

            Map.Get<ConversationManager>().Say(CharacterId, message.AuthorId, "Not now.", false, true);
        }
    }
}
