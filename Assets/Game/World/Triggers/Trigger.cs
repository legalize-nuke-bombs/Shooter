using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(MainTriggerable))]
    public abstract class Trigger : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();
        [SerializeField] private bool allowReiteration = true;

        private readonly HashSet<long> done = new();

        public string ComponentKey => "Trigger";

        private struct SaveData
        {
            public List<long> Done { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveData()
            {
                Done = done.ToList()
            };
        }
        public void LoadComponent(JToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            done.Clear();
            foreach (long id in sd.Done)
            {
                done.Add(id);
            }
        }

        private MainTriggerable triggerable;

        protected virtual void Awake()
        {
            triggerable = GetComponent<MainTriggerable>();
            if (triggerable == null) Log.Warn($"Entity {name} does not have main triggerable");

            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        protected void OnTrigger(Character character)
        {
            if (!allowReiteration)
                if (!done.Add(character.Value))
                    return;

            Log.Info($"Entity {name} triggered on {character.name}");
            triggerable.OnTrigger(character);
        }
    }
}
