using System.Collections.Generic;
using UnityEngine;
using Shooter.Net.Msgs;
using Shooter.Server.Characters;
using Shooter.Logging;

namespace Shooter.Client.Characters
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
            foreach (PlayerState state in snapshot.players)
            {
                if (state.id == NetworkClient.Instance.PlayerId) continue;

                if (!avatars.TryGetValue(state.id, out Avatar avatar))
                    avatar = Spawn(state);

                avatar.TargetPosition = new Vector3(state.x, state.y, state.z);
                avatar.TargetYaw = state.yaw;
            }
        }

        private void OnLeft(LeftMsg left)
        {
            if (!avatars.TryGetValue(left.id, out Avatar avatar)) return;
            Destroy(avatar.Transform.gameObject);
            avatars.Remove(left.id);
            Log.Info("avatar removed for player " + left.id + ", avatars now " + avatars.Count);
        }

        private Avatar Spawn(PlayerState state)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Avatar_" + state.id + "_" + state.name;
            capsule.transform.position = new Vector3(state.x, state.y, state.z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);

            var avatar = new Avatar { Transform = capsule.transform };
            avatars[state.id] = avatar;
            Log.Info("avatar spawned for player " + state.id + " '" + state.name + "', avatars now " + avatars.Count);
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
