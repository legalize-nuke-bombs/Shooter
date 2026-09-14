using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter
{
    [RequireComponent(typeof(NetworkObject))]
    public class ResourceItem : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private GameObject body;
        [SerializeField] private Pickupable bodyPrefab;

        private bool alive = false;
        private float timer;
        [SerializeField] private float respawnDelay = 5f;

        public string ComponentKey => "ResourceItem";
        private struct SaveData
        {
            public bool Alive { get; set; }
            public float Timer { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Alive = alive,
                Timer = timer
            };
        }
        public void LoadObject(SaveToken content)
        {
            // The world is frozen while it loads, so a body saved alive is grown by the first Update after the thaw
            SaveData sd = content.To<SaveData>();
            alive = false;
            timer = sd.Alive ? respawnDelay : sd.Timer;
        }

        private void Awake()
        {
            enabled = NetworkManager.Singleton.IsServer;
        }

        private void Update()
        {
            if (!alive)
            {
                timer += Time.deltaTime;
                if (timer >= respawnDelay)
                {
                    Respawn();
                }
            }
        }

        private void Respawn()
        {
            if (!alive)
            {
                Log.Info($"Entity {name} is spawning its body");
                if (body == null)
                {
                    body = Spawner.Current.Spawn(bodyPrefab.gameObject, transform);
                }
                timer = 0;
                if (body == null)
                {
                    Log.Error($"Entity {name} failed to spawn its body, retrying in {respawnDelay} s");
                    return;
                }
                alive = true;
                body.GetComponent<Pickupable>().OnPickup += MarkDead;
            }
        }

        private void MarkDead(Pickupable pickupable)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback");
            alive = false;
            timer = 0;
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
