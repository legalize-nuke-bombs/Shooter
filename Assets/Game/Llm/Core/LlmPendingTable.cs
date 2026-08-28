using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Core.Saves;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public sealed class LlmPendingTable : MonoBehaviour, ISaveableComponent
    {
        private readonly HashSet<long> pending = new();

        public string ComponentKey => "LlmPendingTable";
        private struct SaveData
        {
            public List<long> Pending { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Pending = pending.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            pending.Clear();
            foreach (long id in sd.Pending) pending.Add(id);
        }

        public bool Any => pending.Count > 0;

        public List<long> Ids()
        {
            return pending.ToList();
        }

        public bool Has(long wandererId)
        {
            return pending.Contains(wandererId);
        }

        public void Mark(long wandererId)
        {
            pending.Add(wandererId);
        }

        public bool Clear(long wandererId)
        {
            return pending.Remove(wandererId);
        }
    }
}
