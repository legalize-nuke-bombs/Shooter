using System;
using System.Collections.Generic;
using Shooter.Server.Worlds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Sleeping;
using Shooter.Client.Worlds.Entities;
using Shooter.Logging;

namespace Shooter.Client.Worlds
{
    public class ClientWorld
    {
        public Guid MyId { get; }
        public ClockState Clock { get; private set; }
        public SleepState Sleep { get; private set; }
        public Dictionary<Guid, EntityState> Entities { get; private set; }

        public EntityState Me
        {
            get
            {
                if (Entities == null) return null;
                return Entities.TryGetValue(MyId, out EntityState me) ? me : null;
            }
        }

        private readonly Dictionary<Guid, EntityView> views = new Dictionary<Guid, EntityView>();
        private readonly List<Guid> departed = new List<Guid>();

        public ClientWorld(Guid myId)
        {
            MyId = myId;
        }

        public void Apply(Snapshot snapshot)
        {
            Clock = snapshot.Clock;
            Sleep = snapshot.Sleep;
            Entities = snapshot.Entities;
            Reconcile(snapshot.Entities);
        }

        public void Tick(float dt)
        {
            foreach (EntityView view in views.Values)
                view.Tick(dt);
        }

        public void Destroy()
        {
            foreach (EntityView view in views.Values)
                view.Destroy();
            views.Clear();
        }

        private void Reconcile(Dictionary<Guid, EntityState> states)
        {
            foreach (KeyValuePair<Guid, EntityState> pair in states)
            {
                if (pair.Key == MyId) continue;
                if (!views.TryGetValue(pair.Key, out EntityView view))
                {
                    view = new EntityView(pair.Value);
                    views[pair.Key] = view;
                    Log.Info("Entity view spawned {}. Total: {}", pair.Key, views.Count);
                }
                else
                {
                    view.Apply(pair.Value);
                }
            }

            departed.Clear();
            foreach (Guid id in views.Keys)
                if (!states.ContainsKey(id))
                    departed.Add(id);
            foreach (Guid id in departed)
            {
                views[id].Destroy();
                views.Remove(id);
                Log.Info("Entity view removed {}. Total: {}", id, views.Count);
            }
        }
    }
}
