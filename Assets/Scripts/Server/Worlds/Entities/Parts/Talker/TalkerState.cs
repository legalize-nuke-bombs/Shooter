using System.Collections.Generic;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class TalkerState : PartState
    {
        public Dictionary<long, ConversationState> Conversations { get; set; }
    }
}
