using System;
using UnityEngine;
using Shooter.Entities.Player;

namespace Shooter.Net
{
    [Serializable]
    public class Envelope
    {
        public string type;
    }

    [Serializable]
    public class HelloMsg
    {
        public string type = "hello";
        public string name;
    }

    [Serializable]
    public class WelcomeMsg
    {
        public string type;
        public long playerId;
        public int tickRate;
    }

    [Serializable]
    public class JoinWorldMsg
    {
        public string type = "joinWorld";
    }

    [Serializable]
    public class WorldJoinedMsg
    {
        public string type;
        public string worldId;
        public PlayerStateMsg[] players;
    }

    [Serializable]
    public class SnapshotMsg
    {
        public string type;
        public long tick;
        public PlayerStateMsg[] players;
    }

    [Serializable]
    public class JoinedMsg
    {
        public string type;
        public long id;
        public string name;
    }

    [Serializable]
    public class LeftMsg
    {
        public string type;
        public long id;
    }

    [Serializable]
    public class UnityHookMsg
    {
        public string action;
        public long userId;
        public string worldId;
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
