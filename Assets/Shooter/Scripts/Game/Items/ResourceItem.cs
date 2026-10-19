using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NetworkObject))]
    public class ResourceItem : MonoBehaviour, ISaveableComponent
    {
        private const double SecondsPerHour = 3600.0;

        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private Clock clock;
        private GameObject body;
        [SerializeField] private Pickupable bodyPrefab;

        private bool alive = false;
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
            SaveData saveData = content.ToObject<SaveData>();
            alive = false;
            takenAt = saveData.Alive ? null : saveData.TakenAt;
        }

        private void Awake()
        {
            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).Single();
            enabled = NetworkManager.Singleton.IsServer;
        }

        private void Update()
        {
            if (alive) return;

            if (takenAt == null || clock.Timestamp - takenAt.Value >= respawnHours * SecondsPerHour)
            {
                SpawnBody();
            }
        }

        private void SpawnBody()
        {
            if (!alive)
            {
                Log.Info($"Entity {name} is spawning its body");
                if (body == null)
                {
                    body = NetworkSpawner.InstantiateAndSpawn(bodyPrefab.gameObject, transform);
                }
                if (body == null)
                {
                    Log.Error($"Entity {name} failed to spawn its body, retrying in {respawnHours} h of world time");
                    takenAt = clock.Timestamp;
                    return;
                }
                alive = true;
                takenAt = null;
                body.GetComponent<Pickupable>().PickedUp += HandleBodyPickedUp;
            }
        }

        private void HandleBodyPickedUp(Pickupable pickupable)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback, grows back in {respawnHours} h of world time");
            alive = false;
            takenAt = clock.Timestamp;
            pickupable.PickedUp -= HandleBodyPickedUp;
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
