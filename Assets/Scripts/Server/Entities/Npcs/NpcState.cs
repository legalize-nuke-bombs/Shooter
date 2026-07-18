using UnityEngine;

namespace Shooter.Server.Entities.Npcs
{
    public class NpcState
    {
        public string Name { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public NpcState(Npc npc)
        {
            Name = npc.Name();

            Vector3 position = npc.Body.transform.position;
            X = position.x;
            Y = position.y;
            Z = position.z;
        }
    }
}
