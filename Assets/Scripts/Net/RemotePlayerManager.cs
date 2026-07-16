using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Net
{
    public class RemotePlayerManager : MonoBehaviour
    {
        private const float LerpFactor = 15f;

        private readonly Dictionary<long, Transform> remotes = new Dictionary<long, Transform>();
        private readonly Dictionary<long, Vector3> targets = new Dictionary<long, Vector3>();
        private readonly Dictionary<long, float> targetYaws = new Dictionary<long, float>();

        private void Start()
        {
            NetworkClient.Instance.SnapshotReceived += OnSnapshot;
            NetworkClient.Instance.PlayerLeft += OnLeft;
        }

        private void OnSnapshot(SnapshotMsg snapshot)
        {
            foreach (PlayerStateMsg player in snapshot.players)
            {
                if (player.id == NetworkClient.Instance.PlayerId) continue;

                if (!remotes.ContainsKey(player.id))
                    Spawn(player);

                targets[player.id] = new Vector3(player.x, player.y, player.z);
                targetYaws[player.id] = player.yaw;
            }
        }

        private void OnLeft(LeftMsg msg)
        {
            if (!remotes.TryGetValue(msg.id, out Transform remote)) return;
            Destroy(remote.gameObject);
            remotes.Remove(msg.id);
            targets.Remove(msg.id);
            targetYaws.Remove(msg.id);
        }

        private void Spawn(PlayerStateMsg player)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Remote_" + player.id + "_" + player.name;
            capsule.transform.position = new Vector3(player.x, player.y, player.z);
            capsule.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.3f);
            remotes[player.id] = capsule.transform;
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-LerpFactor * Time.deltaTime);
            foreach (KeyValuePair<long, Transform> pair in remotes)
            {
                pair.Value.position = Vector3.Lerp(pair.Value.position, targets[pair.Key], t);
                pair.Value.rotation = Quaternion.Slerp(pair.Value.rotation, Quaternion.Euler(0f, targetYaws[pair.Key], 0f), t);
            }
        }

        private void OnDestroy()
        {
            if (NetworkClient.Instance == null) return;
            NetworkClient.Instance.SnapshotReceived -= OnSnapshot;
            NetworkClient.Instance.PlayerLeft -= OnLeft;
        }
    }
}
