using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Shooter.Server.Entities.Players;
using Shooter.Server.Entities.Chronology;
using Shooter.Logging;
using Shooter.Server.Entities.Npcs;
using Shooter.Server.Entities.Npcs.Specs.Nameable;

namespace Shooter.Server.Worlds
{
    public class ServerWorld
    {
        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly Dictionary<long, Player> players = new Dictionary<long, Player>();
        private readonly List<Npc> npcs = new List<Npc>();

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("World " + id + " built: additive physics copy of Map, scene handle " + scene.handle);
            AddNpc(0, new DefaultNameable("npc 0"));
        }

        public IReadOnlyCollection<Player> Players => players.Values;

        public void AddPlayer(long userId, string displayName)
        {
            players[userId] = new Player(userId, displayName, scene);
        }

        public void RemovePlayer(long userId)
        {
            if (players.TryGetValue(userId, out Player player))
            {
                player.Destroy();
                players.Remove(userId);
            }
        }

        public void ApplyInput(long userId, PlayerIntent intent)
        {
            if (players.TryGetValue(userId, out Player player))
                player.ApplyInput(intent);
        }

        public void AddNpc(long id, INameable nameable)
        {
            npcs.Add(new Npc(id, nameable, scene));
        }

        public void Tick(float dt)
        {
            foreach (Player player in players.Values)
                player.Tick(dt);
            clock.Tick(dt);
        }

        public ClockState BuildClockState()
        {
            return clock.State();
        }

        public PlayerState[] BuildPlayerStates()
        {
            var states = new List<PlayerState>(players.Count);
            foreach (Player player in players.Values)
                states.Add(player.State());
            return states.ToArray();
        }

        public NpcState[] BuildNpcStates()
        {
            var states = new List<NpcState>(npcs.Count);
            foreach (Npc npc in npcs)
                states.Add(npc.State());
            return states.ToArray();
        }

        public Snapshot BuildSnapshot(long tick)
        {
            return new Snapshot
            {
                Tick = tick,
                Clock = BuildClockState(),
                Players = BuildPlayerStates(),
                Npcs = BuildNpcStates()
            };
        }
    }
}
