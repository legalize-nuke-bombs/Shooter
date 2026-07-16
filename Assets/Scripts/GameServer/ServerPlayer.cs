using UnityEngine;
using Shooter.Net;

namespace Shooter.GameServer
{
    public class ServerPlayer
    {
        public int ConnId;
        public long UserId = -1;
        public string DisplayName = "";
        public string WorldId = "";
        public bool Authed;
        public bool InRoom;
        public GameObject Body;
        public CharacterController Controller;
        public InputMsg LastInput = new InputMsg();
        public bool JumpQueued;
        public float VerticalVelocity;
    }
}
