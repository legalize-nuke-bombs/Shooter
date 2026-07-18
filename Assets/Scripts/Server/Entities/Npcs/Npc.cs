using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Entities.Npcs.Specs.Nameable;

namespace Shooter.Server.Entities.Npcs
{
    public class Npc
    {

        public long Id { get; private set; }
        public GameObject Body { get; private set; }

        private readonly INameable nameable;

        public Npc(long id, INameable nameable)
        {
            Id = id;
            this.nameable = nameable;
        }

        public void Tick(float dt)
        {

        }

        public void Spawn()
        {
            Body = new GameObject("Npc_" + Id);
            Vector3 spread = Quaternion.Euler(0f, 0f, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            Log.Info("spawned body for npc at " + Body.transform.position);
        }

        public string Name()
        {
            return nameable == null ? "" : nameable.Name();
        }

        public NpcState State()
        {
            Vector3 position = Body.transform.position;
            return new NpcState
            {
                Id = Id,
                Name = Name(),
                X = position.x,
                Y = position.y,
                Z = position.z
            };
        }
    }
}
