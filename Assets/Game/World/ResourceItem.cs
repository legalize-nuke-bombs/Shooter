using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Loot;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter
{
    [RequireComponent(typeof(NetworkObject))]
    public class ResourceItem : MonoBehaviour, ISaveableComponent
    {
        private const double SecondsPerHour = 3600.0;

        private static readonly Journal Log = Logs.Here();

        private GameObject body;
        [SerializeField] private Pickupable bodyPrefab;

        private bool alive = false;
        // World time (clock seconds) when the body was taken; null = never taken, so the body grows at once
        private double? takenAt;
        [SerializeField] private float respawnHours = 48f;

        public string ComponentKey => "ResourceItem";
        private struct SaveData
        {
            public bool Alive { get; set; }
            public double? TakenAt { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Alive = alive,
                TakenAt = takenAt
            };
        }
        public void LoadObject(SaveToken content)
        {
            // The world is frozen while it loads, so a body saved alive is grown by the first Update after the thaw
            SaveData sd = content.To<SaveData>();
            alive = false;
            takenAt = sd.Alive ? null : sd.TakenAt;
        }

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
        }

        private void Update()
        {
            if (alive) return;

            if (takenAt == null || GameState.Get<Clock>().Timestamp - takenAt.Value >= respawnHours * SecondsPerHour)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            if (!alive)
            {
                Log.Info($"Entity {name} is spawning its body");
                if (body == null)
                {
                    body = Spawner.Spawn(bodyPrefab.gameObject, transform);
                }
                if (body == null)
                {
                    Log.Error($"Entity {name} failed to spawn its body, retrying in {respawnHours} h of world time");
                    takenAt = GameState.Get<Clock>().Timestamp;
                    return;
                }
                alive = true;
                takenAt = null;
                body.GetComponent<Pickupable>().OnPickup += MarkDead;
            }
        }

        private void MarkDead(Pickupable pickupable)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback, grows back in {respawnHours} h of world time");
            alive = false;
            takenAt = GameState.Get<Clock>().Timestamp;
            pickupable.OnPickup -= MarkDead;
        }

        private void OnDrawGizmos()
        {
            if (bodyPrefab == null) return;

            Gizmos.color = new Color(0, 1, 1, 0.5f);

            MeshFilter meshFilter = bodyPrefab.GetComponentInChildren<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Transform prefabTransform = meshFilter.transform;
                Vector3 position = transform.TransformPoint(prefabTransform.localPosition);
                Quaternion rotation = transform.rotation * prefabTransform.localRotation;
                Vector3 scale = Vector3.Scale(transform.lossyScale, prefabTransform.localScale);

                Gizmos.DrawMesh(meshFilter.sharedMesh, position, rotation, scale);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
    }
}
