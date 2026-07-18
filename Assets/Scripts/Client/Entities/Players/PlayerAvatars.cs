using System.Collections.Generic;
using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Server.Entities.Players;
using Shooter.Logging;

namespace Shooter.Client.Entities.Players
{
    public class PlayerAvatars : MonoBehaviour
    {

        private const float LerpFactor = 15f;

        private readonly Dictionary<long, PlayerAvatar> dict = new Dictionary<long, PlayerAvatar>();

        private void Start()
        {
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
            NetworkClient.Instance.PeerLeft += OnLeft;
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance == null) return;
            NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
            NetworkClient.Instance.PeerLeft -= OnLeft;
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            foreach (PlayerState state in snapshot.Players)
            {
                if (!dict.TryGetValue(state.Id, out PlayerAvatar avatar))
                    avatar = Spawn(state);

                avatar.TargetPosition = new Vector3(state.X, state.Y, state.Z);
                avatar.TargetYaw = state.Yaw;
            }
        }

        private PlayerAvatar Spawn(PlayerState state)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Player_" + state.Id;
            capsule.transform.position = new Vector3(state.X, state.Y, state.Z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);

            var avatar = new PlayerAvatar { Transform = capsule.transform };
            dict[state.Id] = avatar;
            Log.Info("player avatar spawned " + state.Id + ". total: " + dict.Count);

            return avatar;
        }

        private void OnLeft(PlayerLeft left)
        {
            if (!dict.TryGetValue(left.Id, out PlayerAvatar avatar)) return;

            Destroy(avatar.Transform.gameObject);

            dict.Remove(left.Id);
            Log.Info("player avatar removed " + left.Id + ". total: " + dict.Count);
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-LerpFactor * Time.deltaTime);
            foreach (PlayerAvatar avatar in dict.Values)
            {
                avatar.Transform.position = Vector3.Lerp(avatar.Transform.position, avatar.TargetPosition, t);
                avatar.Transform.rotation = Quaternion.Slerp(avatar.Transform.rotation, Quaternion.Euler(0f, avatar.TargetYaw, 0f), t);
            }
        }
    }
}
