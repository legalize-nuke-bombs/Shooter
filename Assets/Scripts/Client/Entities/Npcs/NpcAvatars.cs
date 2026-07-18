using System.Collections.Generic;
using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Server.Entities.Npcs;
using Shooter.Logging;

namespace Shooter.Client.Entities.Npcs
{
    public class NpcAvatars : MonoBehaviour
    {
        private readonly Dictionary<long, NpcAvatar> avatars = new Dictionary<long, NpcAvatar>();

        private void Start()
        {
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance != null)
                NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
            foreach (NpcAvatar avatar in avatars.Values)
                avatar.Destroy();
            avatars.Clear();
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            foreach (NpcState state in snapshot.Npcs)
            {
                if (!avatars.TryGetValue(state.Id, out NpcAvatar avatar))
                {
                    avatar = new NpcAvatar(state.Id, new Vector3(state.X, state.Y, state.Z));
                    avatars[state.Id] = avatar;
                    Log.Info("Npc avatar spawned " + state.Id + ". Total: " + avatars.Count);
                }
                avatar.Apply(state);
            }
        }

        private void Update()
        {
            foreach (NpcAvatar avatar in avatars.Values)
                avatar.Interpolate(Time.deltaTime);
        }
    }
}
