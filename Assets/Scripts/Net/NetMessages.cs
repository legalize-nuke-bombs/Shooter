using System;
using UnityEngine;

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
public class JoinRoomMsg
{
    public string type = "joinRoom";
    public string code;
}

[Serializable]
public class RoomJoinedMsg
{
    public string type;
    public string roomId;
    public PlayerStateMsg[] players;
}

[Serializable]
public class InputMsg
{
    public string type = "input";
    public int seq;
    public float moveX;
    public float moveZ;
    public bool jump;
    public bool sprint;
    public float yaw;
    public float pitch;
}

[Serializable]
public class SnapshotMsg
{
    public string type;
    public long tick;
    public PlayerStateMsg[] players;
}

[Serializable]
public class PlayerStateMsg
{
    public long id;
    public string name;
    public float x;
    public float y;
    public float z;
    public float yaw;
    public float pitch;
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
