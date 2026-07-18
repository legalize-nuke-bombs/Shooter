using System.Collections.Generic;
using UnityEngine;
using Shooter.Server.Worlds;
using Shooter.Server.Entities.Players;
using Shooter.Logging;

namespace Shooter.Client.Entities.Players
{
    public class PlayersView : MonoBehaviour
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
                if (state.Id == NetworkClient.Instance.PlayerId) continue;

                if (!avatars.TryGetValue(state.Id, out Avatar avatar))
                    avatar = Spawn(state);

                avatar.TargetPosition = new Vector3(state.X, state.Y, state.Z);
                avatar.TargetYaw = state.Yaw;
            }
        }

        private void OnLeft(PlayerLeft left)
        {
            if (!avatars.TryGetValue(left.Id, out Avatar avatar)) return;
            Destroy(avatar.Transform.gameObject);
            avatars.Remove(left.Id);
            Log.Info("avatar removed for player " + left.Id + ", avatars now " + avatars.Count);
        }

        private Avatar Spawn(PlayerState state)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Avatar_" + state.Id + "_" + state.Name;
            capsule.transform.position = new Vector3(state.X, state.Y, state.Z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);

            var avatar = new Avatar { Transform = capsule.transform };
            avatars[state.Id] = avatar;
            Log.Info("avatar spawned for player " + state.Id + " '" + state.Name + "', avatars now " + avatars.Count);
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
