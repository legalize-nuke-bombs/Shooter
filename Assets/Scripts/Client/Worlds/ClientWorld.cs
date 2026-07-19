using System.Collections.Generic;
using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Npcs;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Sleeping;
using Shooter.Client.Entities.Players;
using Shooter.Client.Entities.Npcs;
using Shooter.Logging;

namespace Shooter.Client.Worlds
{
    public class ClientWorld
    {
        public long PlayerId { get; }
        public ClockState Clock { get; private set; }
        public SleepState Sleep { get; private set; }
        public Dictionary<long, PlayerState> Players { get; private set; }

        public PlayerState Me
        {
            get
            {
                if (Players == null) return null;
                return Players.TryGetValue(PlayerId, out PlayerState me) ? me : null;
            }
        }

        private readonly Dictionary<long, PlayerAvatar> peers = new Dictionary<long, PlayerAvatar>();
        private readonly Dictionary<long, NpcAvatar> npcs = new Dictionary<long, NpcAvatar>();
        private readonly List<long> departed = new List<long>();

        public ClientWorld(long playerId)
        {
            PlayerId = playerId;
        }

        public void Apply(Snapshot snapshot)
        {
            Clock = snapshot.Clock;
            Sleep = snapshot.Sleep;
            Players = snapshot.Players;
            ReconcilePeers(snapshot.Players);
            ReconcileNpcs(snapshot.Npcs);
        }

        public void Interpolate(float dt)
        {
            foreach (PlayerAvatar peer in peers.Values)
                peer.Interpolate(dt);
            foreach (NpcAvatar npc in npcs.Values)
                npc.Interpolate(dt);
        }

        public void Destroy()
        {
            foreach (PlayerAvatar peer in peers.Values)
                peer.Destroy();
            peers.Clear();
            foreach (NpcAvatar npc in npcs.Values)
                npc.Destroy();
            npcs.Clear();
        }

        private void ReconcilePeers(Dictionary<long, PlayerState> states)
        {
            foreach (KeyValuePair<long, PlayerState> pair in states)
            {
                if (pair.Key == PlayerId) continue;
                if (!peers.TryGetValue(pair.Key, out PlayerAvatar peer))
                {
                    PlayerState state = pair.Value;
                    peer = new PlayerAvatar(state.Id, new Vector3(state.X, state.Y, state.Z));
                    peers[pair.Key] = peer;
                    Log.Info("Player avatar spawned {}. Total: {}", state.Id, peers.Count);
                }
                peer.Apply(pair.Value);
            }

            departed.Clear();
            foreach (long id in peers.Keys)
                if (!states.ContainsKey(id))
                    departed.Add(id);
            foreach (long id in departed)
            {
                peers[id].Destroy();
                peers.Remove(id);
                Log.Info("Player avatar removed {}. Total: {}", id, peers.Count);
            }
        }

        private void ReconcileNpcs(List<NpcState> states)
        {
            foreach (NpcState state in states)
            {
                if (!npcs.TryGetValue(state.Id, out NpcAvatar npc))
                {
                    npc = new NpcAvatar(state.Id, new Vector3(state.X, state.Y, state.Z));
                    npcs[state.Id] = npc;
                    Log.Info("Npc avatar spawned {}. Total: {}", state.Id, npcs.Count);
                }
                npc.Apply(state);
            }

            departed.Clear();
            foreach (long id in npcs.Keys)
            {
                bool present = false;
                foreach (NpcState state in states)
                    if (state.Id == id)
                    {
                        present = true;
                        break;
                    }
                if (!present)
                    departed.Add(id);
            }
            foreach (long id in departed)
            {
                npcs[id].Destroy();
                npcs.Remove(id);
                Log.Info("Npc avatar removed {}. Total: {}", id, npcs.Count);
            }
        }
    }
}
