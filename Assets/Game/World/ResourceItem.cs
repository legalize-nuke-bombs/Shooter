using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter
{
    public class ResourceItem : MonoBehaviour, IUsable, ISaveableComponent, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private NetworkObject body;
        [SerializeField] private SoundSpec sound;
        [SerializeField] private StackableItemSpec item;

        private float timer;
        [SerializeField] private float respawnDelay = 5f;

        public bool Alive => body.IsSpawned;

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
                Alive = Alive,
                Timer = timer
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            SetStatus(sd.Alive);
            timer = sd.Timer;
        }

        private void Awake()
        {
            if (body == null)
            {
                Log.Error($"Entity {name} does not have set body");
            }
            if (sound == null)
            {
                Log.Warn($"Entity {name} does not have set sound");
            }
            if (item == null)
            {
                Log.Error($"Entity {name} does not have set item");
            }
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }
            if (!Alive)
            {
                timer += Time.deltaTime;
                if (timer >= respawnDelay)
                {
                    SetStatus(true);
                }
            }
        }

        public UsageType Usage => UsageType.Talk;
        public void Use(NetworkObject user)
        {
            Log.Info($"Entity {name} was been picked up by {user.name}");
            if (user.TryGetComponent(out Speaker speaker))
            {
                speaker.Play(sound);
            }
            else
            {
                Log.Warn($"Failed to find speaker of user {user.name}");
            }
            if (user.TryGetComponent(out Inventory inventory))
            {
                inventory.AddStackable(item, 1);
            }
            else
            {
                Log.Warn($"Failed to find inventory of user {user.name}");
            }
            SetStatus(false);
        }

        private void SetStatus(bool alive)
        {
            if (alive)
            {
                if (!body.IsSpawned)
                {
                    Log.Info($"Entity {name} is enabling its body");
                    body.gameObject.SetActive(true);
                    body.Spawn(true);
                    timer = 0;
                }
            }
            else
            {
                if (body.IsSpawned)
                {
                    Log.Info($"Entity {name} is disabling its body");
                    body.Despawn(!body.InScenePlaced);
                    body.gameObject.SetActive(false);
                    timer = 0;
                }
            }
        }

        public DigestionPriority Priority => DigestionPriority.High;
        public string Digest(DigestionDetail detail)
        {
            if (Alive)
            {
                return $"{item.Id}. Can be picked up";
            }
            return null;
        }
    }
}
