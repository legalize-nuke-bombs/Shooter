using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.CharSpecs.Nameable;
using Shooter.Server.Worlds.Entities.CharSpecs.Living;

namespace Shooter.Server.Worlds.Entities.Npcs
{
    public class Npc
    {
        public long Id { get; }
        public GameObject Body { get; private set; }

        private readonly INameable nameable;
        private readonly ILiving living;

        public Npc(long id, INameable nameable, ILiving living, Scene scene)
        {
            Id = id;
            this.nameable = nameable;
            this.living = living;

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

        public NpcState State()
        {
            Vector3 position = Body.transform.position;
            return new NpcState
            {
                Id = Id,
                Name = nameable.Name(),
                Alive = living.Alive(),
                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = Body.transform.eulerAngles.y
            };
        }
    }
}
