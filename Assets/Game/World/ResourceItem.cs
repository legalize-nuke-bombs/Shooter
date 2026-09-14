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
            SaveData sd = content.To<SaveData>();
            if (sd.Alive)
            {
                Respawn();
            }
            timer = sd.Timer;
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }
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
                alive = true;
                body.GetComponent<Pickupable>().OnPickup += MarkDead;
                timer = 0;
            }
        }

        private void MarkDead(Pickupable pickupable)
        {
            Log.Info($"Entity {name} (pickable {pickupable.name}) became dead via callback");
            alive = false;
            pickupable.OnPickup -= MarkDead;
        }
    }
}
