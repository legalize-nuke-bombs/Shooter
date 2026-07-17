using System;
using UnityEngine;

namespace Shooter.Entities.Characters
{
    [Serializable]
    public class PlayerState
    {
        public long id;
        public string name;
        public float x;
        public float y;
        public float z;
        public float yaw;
        public float pitch;

        public PlayerState() { }

        public PlayerState(Player player)
        {
            Vector3 position = player.Body.transform.position;
            id = player.UserId;
            name = player.DisplayName;
            x = position.x;
            y = position.y;
            z = position.z;
            yaw = player.Body.transform.eulerAngles.y;
            pitch = player.LastInput.pitch;
        }
    }
}
