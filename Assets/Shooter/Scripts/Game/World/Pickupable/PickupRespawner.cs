using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NetworkObject))]
    public class PickupRespawner : ShooterBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Clock clock;
        private GameObject body;
        [SerializeField] private ItemAsset item;
        [SerializeField] private Pickupable bodyPrefab;
        [SerializeField] private bool growsBack = true;

        private bool alive = false;
        private double? rolledUntil;
        [SerializeField] private float averageHoursToGrow = 48f;

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
            // The world is frozen while it loads, so a body saved alive is grown in Start, which a frozen respawner gets only after the thaw
            SaveData saveData = content.ToObject<SaveData>();
            alive = false;
            rolledUntil = saveData.Alive ? null : saveData.RolledUntil;
        }

        protected override void ShooterAwake()
        {
            clock = Registers.Get<Clock>();
            enabled = NetworkManager.Singleton.IsServer;
            if (item == null)
            {
                Log.Error($"Entity {name} is missing its item");
            }
        }

        private void Start()
        {
            if (rolledUntil == null) SpawnBody();
        }

        private void Update()
        {
            if (alive) return;

            if (!growsBack) return;

            // One roll per hour of world time; hours skipped at once, as by sleeping, get one roll with their joint chance
            int hours = (int)((clock.Timestamp - rolledUntil.Value) / Clock.HourLengthSeconds);
            if (hours < 1) return;

            rolledUntil += hours * Clock.HourLengthSeconds;
            float chance = 1f - Mathf.Pow(1f - 1f / averageHoursToGrow, hours);
            if (Random.value < chance) SpawnBody();
        }

        private void SpawnBody()
        {
            Log.Info($"Entity {name} is spawning its body of {item.Id}");
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
            Pickupable pickupable = body.GetComponent<Pickupable>();
            pickupable.AssignItem(item);
            pickupable.PickedUp += HandleBodyPickedUp;
        }

        private void HandleBodyPickedUp(Pickupable pickupable, NetworkObject user)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback, {(growsBack ? $"grows back in about {averageHoursToGrow} h of world time" : "does not grow back")}");
            alive = false;
            rolledUntil = clock.Timestamp;
            pickupable.PickedUp -= HandleBodyPickedUp;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f);

            MeshFilter meshFilter = item == null || item.Model == null ? null : item.Model.GetComponentInChildren<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Transform modelTransform = meshFilter.transform;
                Vector3 position = transform.TransformPoint(modelTransform.localPosition);
                Quaternion rotation = transform.rotation * modelTransform.localRotation;
                Vector3 scale = Vector3.Scale(transform.lossyScale, modelTransform.localScale);

                Gizmos.DrawMesh(meshFilter.sharedMesh, position, rotation, scale);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
    }
}
