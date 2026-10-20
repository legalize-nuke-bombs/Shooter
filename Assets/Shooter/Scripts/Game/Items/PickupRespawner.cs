using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NetworkObject))]
    public class PickupRespawner : ShooterBehaviour, ISaveableComponent
    {
        private const double SecondsPerHour = 3600.0;

        private static readonly Journal Log = Logs.Here();

        private Clock clock;
        private GameObject body;
        [SerializeField] private Pickupable bodyPrefab;

        private bool alive = false;
        private double? rolledUntil;
        [SerializeField] private float averageHoursToGrow = 48f;

        public string ComponentKey => "ResourceItem";
        private struct SaveData
        {
            public bool Alive { get; set; }
            public double? RolledUntil { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Alive = alive,
                RolledUntil = rolledUntil
            };
        }
        public void LoadObject(SaveToken content)
        {
            // The world is frozen while it loads, so a body saved alive is grown by the first Update after the thaw
            SaveData saveData = content.ToObject<SaveData>();
            alive = false;
            rolledUntil = saveData.Alive ? null : saveData.RolledUntil;
        }

        protected override void ShooterAwake()
        {
            clock = Registers.Get<Clock>();
            enabled = NetworkManager.Singleton.IsServer;
        }

        private void Update()
        {
            if (alive) return;

            if (rolledUntil == null)
            {
                SpawnBody();
                return;
            }

            // One roll per hour of world time; hours skipped at once, as by sleeping, get one roll with their joint chance
            int hours = (int)((clock.Timestamp - rolledUntil.Value) / SecondsPerHour);
            if (hours < 1) return;

            rolledUntil += hours * SecondsPerHour;
            float chance = 1f - Mathf.Pow(1f - 1f / averageHoursToGrow, hours);
            if (Random.value < chance) SpawnBody();
        }

        private void SpawnBody()
        {
            Log.Info($"Entity {name} is spawning its body");
            if (body == null)
            {
                body = NetworkSpawner.InstantiateAndSpawn(bodyPrefab.gameObject, transform);
            }
            if (body == null)
            {
                Log.Error($"Entity {name} failed to spawn its body, it will try to grow it again");
                rolledUntil = clock.Timestamp;
                return;
            }
            alive = true;
            rolledUntil = null;
            body.GetComponent<Pickupable>().PickedUp += HandleBodyPickedUp;
        }

        private void HandleBodyPickedUp(Pickupable pickupable, NetworkObject user)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback, grows back in about {averageHoursToGrow} h of world time");
            alive = false;
            rolledUntil = clock.Timestamp;
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
