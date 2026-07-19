using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Npcs;
using Shooter.Server.Worlds.Utils.CharSpecs.Nameable;
using Shooter.Server.Worlds.Utils.CharSpecs.Living;
using Shooter.Server.Worlds.Entities.Sleeping;

namespace Shooter.Server.Worlds
{
    public class ServerWorld
    {
        private const int NpcMaxHp = 1000;

        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly Players players;
        private readonly Sleep sleep;
        private readonly List<Npc> npcs = new List<Npc>();

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("World " + id + " built: additive physics copy of Map, scene handle " + scene.handle);
            players = new Players(scene, clock);
            sleep = new Sleep(clock, players);
            npcs.Add(new Npc(1, new CorruptedNameable(), new DefaultLiving(NpcMaxHp), scene));
        }

        public void Destroy()
        {
            players.DestroyAll();
            foreach (Npc npc in npcs)
                npc.Destroy();
            npcs.Clear();
            SceneManager.UnloadSceneAsync(scene);
            Log.Info("World {} destroyed, scene unloaded", Id);
        }

        public int Online()
        {
            return players.Count();
        }

        public void AddPlayer(long userId, string displayName)
        {
            players.Add(userId, displayName);
        }

        public void RemovePlayer(long userId)
        {
            players.Remove(userId);
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            players.ApplyInput(userId, intent);
        }

        public void Tick(float dt)
        {
            players.Tick(dt);
            clock.Tick(dt * sleep.ClockScale());
            sleep.Tick();
        }

        public ClockState BuildClockState()
        {
            return clock.State();
        }

        public Dictionary<long, PlayerState> BuildPlayerStates()
        {
            return players.BuildStates();
        }

        public List<NpcState> BuildNpcStates()
        {
            var states = new List<NpcState>(npcs.Count);
            foreach (Npc npc in npcs)
                states.Add(npc.State());
            return states;
        }

        public Snapshot BuildSnapshot(long tick)
        {
            return new Snapshot
            {
                Tick = tick,
                Clock = BuildClockState(),
                Players = BuildPlayerStates(),
                Npcs = BuildNpcStates(),
                Sleep = sleep.State()
            };
        }
    }
}
