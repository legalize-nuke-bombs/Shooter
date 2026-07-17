using System;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class UnityHookMsg
    {
        public string action;
        public long userId;
        public string worldId;
    }
}
