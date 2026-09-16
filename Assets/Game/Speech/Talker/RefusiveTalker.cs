using Shooter.Game.Core;

namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) ConversationManager.Current.Said += Refuse;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && ConversationManager.Current != null) ConversationManager.Current.Said -= Refuse;
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

            ConversationManager.Current.Say(CharacterId, message.AuthorId, "Not now.", false, true);
        }
    }
}
