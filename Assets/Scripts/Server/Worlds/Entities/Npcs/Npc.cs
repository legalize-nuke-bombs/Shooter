using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Npcs.Specs.Nameable;

namespace Shooter.Server.Worlds.Entities.Npcs
{
    public class Npc
    {
        public long Id { get; }
        public GameObject Body { get; private set; }

        private readonly INameable nameable;

        public Npc(long id, INameable nameable, Scene scene)
        {
            Id = id;
            this.nameable = nameable;

            Body = new GameObject("Npc_" + id);
            Vector3 spread = Quaternion.Euler(0f, 0f, 0f) * Vector3.forward * 16f;
            Body.transform.position = new Vector3(spread.x, 1.1f, spread.z);
            SceneManager.MoveGameObjectToScene(Body, scene);
            Log.Info("Npc " + id + " body spawned at " + Body.transform.position);
        }

        public void Tick(float dt)
        {

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
                Z = position.z,
                Yaw = Body.transform.eulerAngles.y
            };
        }
    }
}
