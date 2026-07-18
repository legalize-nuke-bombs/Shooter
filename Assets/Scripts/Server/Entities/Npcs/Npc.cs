using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Entities.Npcs.Specs.Nameable;

namespace Shooter.Server.Entities.Npcs
{
    public class Npc
    {
        public GameObject Body { get; private set; }

        private readonly INameable nameable;

        public Npc(INameable nameable)
        {
            this.nameable = nameable;
        }

        public void Tick(float dt)
        {

        }

        public void Spawn()
        {
            Body = new GameObject("Npc");
            Vector3 spread = Quaternion.Euler(0f, 0f, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            Log.Info("spawned body for npc at " + Body.transform.position);
        }

        public string Name()
        {
            return nameable == null ? "" : nameable.Name();
        }
    }
}
