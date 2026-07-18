using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Shooter.Server.Entities.Players;
using Shooter.Server.Entities.Chronology;
using Shooter.Logging;

namespace Shooter.Server.Worlds
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

        public void AddPlayer(Player player)
        {
            players[player.ConnId] = player;
            player.Spawn();
            SceneManager.MoveGameObjectToScene(player.Body, scene);
        }

        public void RemovePlayer(int connId)
        {
            players.Remove(connId);
        }

        public void Tick(float dt)
        {
            foreach (Player player in players.Values)
                player.Tick(dt);
            clock.Tick(dt);
        }

        public PlayerState[] BuildPlayerStates()
        {
            var states = new List<PlayerState>(players.Count);
            foreach (Player player in players.Values)
                states.Add(new PlayerState(player));
            return states.ToArray();
        }

        public ClockState BuildClockState()
        {
            return new ClockState(clock);
        }
    }
}
