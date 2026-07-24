using System.Collections.Generic;
using Shooter.Logging;

namespace Shooter.Server.Worlds
{
    public sealed class ServerWorlds
    {
        private readonly Dictionary<string, ServerWorld> worlds = new Dictionary<string, ServerWorld>();

        public ServerWorld Open(string worldId)
        {
            if (worlds.TryGetValue(worldId, out ServerWorld world)) return world;

            world = new ServerWorld(worldId);
            worlds[worldId] = world;
            Log.Info("World {} opened, total worlds {}", worldId, worlds.Count);
            return world;
        }

        public bool TryGet(string worldId, out ServerWorld world)
        {
            return worlds.TryGetValue(worldId, out world);
        }

        public void CloseWhenEmpty(string worldId)
        {
            if (!worlds.TryGetValue(worldId, out ServerWorld world)) return;
            if (world.Online > 0) return;

            world.Destroy();
            worlds.Remove(worldId);
            Log.Info("World {} evicted, empty, total worlds {}", worldId, worlds.Count);
        }

        public void CloseAll()
        {
            foreach (ServerWorld world in worlds.Values)
                world.Destroy();

            worlds.Clear();
            Log.Info("All worlds closed");
        }

        public void Tick(float dt)
        {
            foreach (ServerWorld world in worlds.Values)
                world.Tick(dt);
        }

        public IEnumerable<ServerWorld> Populated()
        {
            foreach (ServerWorld world in worlds.Values)
                if (world.Online > 0)
                    yield return world;
        }
    }
}
