using System;
using System.Collections.Generic;
using UnityEngine;
using Shooter.Client.Worlds.Entities;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Time;

namespace Shooter.Client.Worlds
{
    public class ClientWorld
    {
        private readonly Transform ownBody;
        private readonly Dictionary<Guid, EntityView> views = new Dictionary<Guid, EntityView>();
        private readonly List<Guid> departed = new List<Guid>();

        public ClientWorld(Guid myId, long myUserId, Transform ownBody)
        {
            MyId = myId;
            MyUserId = myUserId;
            this.ownBody = ownBody;
        }

        public Guid MyId { get; }

        public long MyUserId { get; }

        public ClockState Clock { get; private set; }

        public SleepState Sleep { get; private set; }

        public EntityView Me => views.TryGetValue(MyId, out EntityView me) ? me : null;

        public bool WorldAsleep => Sleep != null && Sleep.WorldAsleep;

        public void Apply(Snapshot snapshot)
        {
            Clock = snapshot.Clock;
            Sleep = snapshot.Sleep;
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
                if (views.TryGetValue(pair.Key, out EntityView view))
                {
                    view.Apply(pair.Value);
                    continue;
                }

                views[pair.Key] = Spawn(pair.Value);
                Log.Info("Entity view spawned {}, total {}", pair.Key, views.Count);
            }

            departed.Clear();
            foreach (Guid id in views.Keys)
                if (!states.ContainsKey(id))
                    departed.Add(id);

            foreach (Guid id in departed)
            {
                views[id].Destroy();
                views.Remove(id);
                Log.Info("Entity view removed {}, total {}", id, views.Count);
            }
        }

        private EntityView Spawn(EntityState state)
        {
            if (state.Id == MyId) return new OwnEntityView(state, ownBody);

            return new OtherEntityView(state);
        }
    }
}
