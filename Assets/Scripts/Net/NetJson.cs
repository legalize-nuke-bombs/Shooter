using System;
using UnityEngine;

namespace Shooter.Net
{
    [Serializable]
    public class Envelope
    {
        public string type;
    }

    public static class NetJson
    {
        public static string PeekType(string json)
        {
            return JsonUtility.FromJson<Envelope>(json).type;
        }

        public static T Parse<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        public static string Serialize(object msg)
        {
            return JsonUtility.ToJson(msg);
        }
    }
}
