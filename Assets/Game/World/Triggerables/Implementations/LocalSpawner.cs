using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    public class LocalSpawner : MonoBehaviour, ITriggerable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject destination;

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

            Log.Info($"Entity {name} is going to spawn {prefab.name}...");
            Spawner spawner = Spawner.Current;
            spawner.Spawn(prefab, destination.transform.position, destination.transform.rotation);
        }
    }
}
