using Shooter.Game.Core;

namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) GameState.Get<ConversationManager>().Said += Refuse;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && GameState.Get<ConversationManager>() != null) GameState.Get<ConversationManager>().Said -= Refuse;
            base.OnNetworkDespawn();
        }

        protected override bool Busy()
        {
            return false;
        }

        private void Refuse(Conversation conversation, Message message)
        {
            if (message.AuthorId == CharacterId || conversation.Partner(message.AuthorId) != CharacterId) return;

            Character author = Character.Of(message.AuthorId, Inactive.Include);
            if (author == null || !author.TryGetComponent(out Player _)) return;

            GameState.Get<ConversationManager>().Say(CharacterId, message.AuthorId, "Not now.", false, true);
        }
    }
}
