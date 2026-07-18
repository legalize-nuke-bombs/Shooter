using System.Collections.Generic;
using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Server.Entities.Players;
using Shooter.Logging;

namespace Shooter.Client.Entities.Players
{
    public class PlayerAvatars : MonoBehaviour
    {
        private readonly Dictionary<long, PlayerAvatar> avatars = new Dictionary<long, PlayerAvatar>();

        private void Start()
        {
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
            NetworkClient.Instance.PeerLeft += OnLeft;
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance != null)
            {
                NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
                NetworkClient.Instance.PeerLeft -= OnLeft;
            }
            foreach (PlayerAvatar avatar in avatars.Values)
                avatar.Destroy();
            avatars.Clear();
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            foreach (PlayerState state in snapshot.Players)
            {
                var position = new Vector3(state.X, state.Y, state.Z);
                if (!avatars.TryGetValue(state.Id, out PlayerAvatar avatar))
                {
                    avatar = new PlayerAvatar(state.Id, position);
                    avatars[state.Id] = avatar;
                    Log.Info("Player avatar spawned " + state.Id + ". Total: " + avatars.Count);
                }
                avatar.SetTarget(position, state.Yaw);
            }
        }

        private void OnLeft(PlayerLeft left)
        {
            if (!avatars.TryGetValue(left.Id, out PlayerAvatar avatar)) return;
            avatar.Destroy();
            avatars.Remove(left.Id);
            Log.Info("Player avatar removed " + left.Id + ". Total: " + avatars.Count);
        }

        private void Update()
        {
            foreach (PlayerAvatar avatar in avatars.Values)
                avatar.Interpolate(Time.deltaTime);
        }
    }
}
