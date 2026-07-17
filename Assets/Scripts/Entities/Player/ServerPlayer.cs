using UnityEngine;

namespace Shooter.Entities.Player
{
    public class ServerPlayer
    {
        public int ConnId;
        public long UserId = -1;
        public string DisplayName = "";
        public string WorldId = "";
        public bool InWorld;
        public GameObject Body;
        public CharacterController Controller;
        public InputMsg LastInput = new InputMsg();
        public bool JumpQueued;
        public float VerticalVelocity;
    }
}
