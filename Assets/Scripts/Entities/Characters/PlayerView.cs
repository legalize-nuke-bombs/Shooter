using System.Collections.Generic;
using UnityEngine;
using Shooter.Net;
using Shooter.Net.Msgs;
using Shooter.Logging;

namespace Shooter.Entities.Characters
{
    public class PlayerView : MonoBehaviour
    {
        private const float LerpFactor = 15f;

        private class Avatar
        {
            public Transform Transform;
            public Vector3 TargetPosition;
            public float TargetYaw;
        }

        private readonly Dictionary<long, Avatar> avatars = new Dictionary<long, Avatar>();

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
            foreach (PlayerState player in snapshot.players)
            {
                if (player.id == NetworkClient.Instance.PlayerId) continue;

                if (!avatars.TryGetValue(player.id, out Avatar avatar))
                    avatar = Spawn(player);

                avatar.TargetPosition = new Vector3(player.x, player.y, player.z);
                avatar.TargetYaw = player.yaw;
            }
        }

        private void OnLeft(LeftMsg msg)
        {
            if (!avatars.TryGetValue(msg.id, out Avatar avatar)) return;
            Destroy(avatar.Transform.gameObject);
            avatars.Remove(msg.id);
            Log.Info("avatar removed: player " + msg.id + ", avatars now " + avatars.Count);
        }

        private Avatar Spawn(PlayerState player)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Avatar_" + player.id + "_" + player.name;
            capsule.transform.position = new Vector3(player.x, player.y, player.z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);

            var avatar = new Avatar { Transform = capsule.transform };
            avatars[player.id] = avatar;
            Log.Info("avatar spawned: player " + player.id + " '" + player.name + "', avatars now " + avatars.Count);
            return avatar;
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-LerpFactor * Time.deltaTime);
            foreach (Avatar avatar in avatars.Values)
            {
                avatar.Transform.position = Vector3.Lerp(avatar.Transform.position, avatar.TargetPosition, t);
                avatar.Transform.rotation = Quaternion.Slerp(avatar.Transform.rotation, Quaternion.Euler(0f, avatar.TargetYaw, 0f), t);
            }
        }
    }
}
