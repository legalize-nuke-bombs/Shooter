using System.Collections.Generic;
using UnityEngine;
using Shooter.Net;

namespace Shooter.Entities.Player
{
    public class RemotePlayerManager : MonoBehaviour
    {
        private const float LerpFactor = 15f;

        private class RemoteAvatar
        {
            public Transform Transform;
            public Vector3 TargetPosition;
            public float TargetYaw;
        }

        private readonly Dictionary<long, RemoteAvatar> remotes = new Dictionary<long, RemoteAvatar>();

        private void Start()
        {
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
            NetworkClient.Instance.PlayerLeft += OnLeft;
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance == null) return;
            NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
            NetworkClient.Instance.PlayerLeft -= OnLeft;
        }

        private void OnSnapshot(SnapshotMsg snapshot)
        {
            foreach (PlayerStateMsg player in snapshot.players)
            {
                if (player.id == NetworkClient.Instance.PlayerId) continue;

                if (!remotes.TryGetValue(player.id, out RemoteAvatar avatar))
                    avatar = Spawn(player);

                avatar.TargetPosition = new Vector3(player.x, player.y, player.z);
                avatar.TargetYaw = player.yaw;
            }
        }

        private void OnLeft(LeftMsg msg)
        {
            if (!remotes.TryGetValue(msg.id, out RemoteAvatar avatar)) return;
            Destroy(avatar.Transform.gameObject);
            remotes.Remove(msg.id);
        }

        private RemoteAvatar Spawn(PlayerStateMsg player)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Remote_" + player.id + "_" + player.name;
            capsule.transform.position = new Vector3(player.x, player.y, player.z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);

            var avatar = new RemoteAvatar { Transform = capsule.transform };
            remotes[player.id] = avatar;
            return avatar;
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-LerpFactor * Time.deltaTime);
            foreach (RemoteAvatar avatar in remotes.Values)
            {
                avatar.Transform.position = Vector3.Lerp(avatar.Transform.position, avatar.TargetPosition, t);
                avatar.Transform.rotation = Quaternion.Slerp(avatar.Transform.rotation, Quaternion.Euler(0f, avatar.TargetYaw, 0f), t);
            }
        }
    }
}
