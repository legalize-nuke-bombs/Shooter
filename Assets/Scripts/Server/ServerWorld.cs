using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Server.Characters;
using Shooter.Server.Chronology;
using Shooter.Net.Msgs;
using Shooter.Logging;

namespace Shooter.Server
{
    public class ServerWorld
    {
        public string Id { get; }

        private readonly Scene scene;
        private readonly Clock clock = new Clock();
        private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

        public ServerWorld(string id)
        {
            Id = id;
            scene = SceneManager.LoadScene("Map", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            Log.Info("world " + id + " built: additive physics copy of Map, scene handle " + scene.handle);
        }

        public IReadOnlyCollection<Player> Players => players.Values;

        public void Add(Player player)
        {
            players[player.ConnId] = player;
            player.Spawn();
            SceneManager.MoveGameObjectToScene(player.Body, scene);
        }

        public void Remove(int connId)
        {
            players.Remove(connId);
        }

        public void Step(float dt)
        {
            foreach (Player player in players.Values)
                player.Step(dt);
            clock.Advance(dt);
        }

        public PlayerState[] BuildStates()
        {
            var states = new List<PlayerState>(players.Count);
            foreach (Player player in players.Values)
                states.Add(new PlayerState(player));
            return states.ToArray();
        }

        public SnapshotMsg BuildSnapshot(long tick)
        {
            return new SnapshotMsg
            {
                tick = tick,
                players = BuildStates(),
                clock = new ClockState(clock)
            };
        }
    }
}
