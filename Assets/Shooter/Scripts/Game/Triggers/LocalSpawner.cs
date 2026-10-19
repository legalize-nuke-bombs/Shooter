using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class LocalSpawner : MonoBehaviour, ITriggerable, ISaveableComponent
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject destination;
        [SerializeField] private bool once = true;

        private bool fired = false;

        public string ComponentKey => "LocalSpawner";
        private struct SaveData
        {
            public bool Fired { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Fired = fired
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            fired = sd.Fired;
        }

        public void OnTrigger(Character character)
        {
            if (prefab == null)
            {
                Log.Warn($"Entity {name} does not have set prefab");
                return;
            }
            if (destination == null)
            {
                Log.Warn($"Entity {name} does not have set destination");
                return;
            }

            if (once && fired)
            {
                return;
            }
            fired = true;

            Log.Info($"Entity {name} is going to spawn {prefab.name}...");
            NetworkSpawner.InstantiateAndSpawn(prefab, destination.transform.position, destination.transform.rotation);
        }
    }
}
