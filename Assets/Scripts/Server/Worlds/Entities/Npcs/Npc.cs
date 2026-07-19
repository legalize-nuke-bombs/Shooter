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
            Body.transform.position = new Vector3(0f, 1.1f, 16f);
            SceneManager.MoveGameObjectToScene(Body, scene);
            Log.Info("Npc " + id + " body spawned at " + Body.transform.position);
        }

        public void Destroy()
        {
            Object.Destroy(Body);
            Body = null;
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
