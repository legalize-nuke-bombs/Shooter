using System;

namespace Shooter.Net
{
    [Serializable]
    public class UnityHookMsg
    {
        public string action;
        public long userId;
        public string worldId;
    }
}
