using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class IncrementalIds : ShooterBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private long nextId;

        protected override void ShooterAwake()
        {
            SaveManager.SnapshotLoaded += RenumberIdsMissingFromSave;
        }

        protected override void ShooterOnDestroy()
        {
            SaveManager.SnapshotLoaded -= RenumberIdsMissingFromSave;
        }

        private struct SaveData
        {
            public long Next { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Next = nextId
            };
        }

        public void LoadObject(SaveToken content)
        {
            nextId = content.ToObject<SaveData>().Next;
        }

        public long TakeNextId()
        {
            return nextId++;
        }

        // The scene spawns and takes its ids before the save is read: an entity the save has no id for holds one taken
        // while the counter was not restored yet, and someone from the save may have the same
        private void RenumberIdsMissingFromSave()
        {
            int renumbered = 0;
            foreach (IncrementalId incrementalId in Registers.GetAll<IncrementalId>(RegisterInactivePolicy.Include))
            {
                if (incrementalId.IsLoadedFromSave) continue;

                long newId = TakeNextId();
                Log.Info($"Entity {incrementalId.name} takes id {newId} instead of {incrementalId.Value}: the save has none for it");
                incrementalId.Renumber(newId);
                renumbered++;
            }

            Log.Info($"Ids missing from the save renumbered: {renumbered}, next id {nextId}");
        }
    }
}
