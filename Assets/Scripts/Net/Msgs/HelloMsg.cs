using System;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class HelloMsg
    {
        public string type = "hello";
        public string name;
    }
}
