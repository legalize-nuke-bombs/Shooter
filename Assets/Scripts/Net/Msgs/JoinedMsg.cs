using System;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class JoinedMsg
    {
        public string type;
        public long id;
        public string name;
    }
}
