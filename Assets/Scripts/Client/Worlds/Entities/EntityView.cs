using System;
using UnityEngine;
using Shooter.Client.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Sleeper;
using Shooter.Server.Worlds.Entities.Parts.Speaker;
using Shooter.Server.Worlds.Entities.Parts.Talker;
using Shooter.Server.Worlds.Items;

namespace Shooter.Client.Worlds.Entities
{
    public abstract class EntityView
    {
        private readonly NameMapper nameMapper = new NameMapper();

        private TalkerState talker;
        private InventoryState inventory;

        protected EntityView(EntityState state)
        {
            Id = state.Id;
            Position = new Vector3(state.X, state.Y, state.Z);
        }

        public Guid Id { get; }

        public string Name { get; private set; }

        public Vector3 Position { get; private set; }

        public float Yaw { get; private set; }

        public bool Alive { get; private set; } = true;

        public bool Sleeping { get; private set; }

        public bool Piloted { get; private set; }

        public long UserId { get; private set; }

        public int Hp { get; private set; }

        public int MaxHp { get; private set; }

        public bool Talkative => talker != null;

        public HandsAction HandsAction { get; private set; } = HandsAction.None;

        public InventoryState Inventory => inventory;

        public UniqueItemState Equipped
        {
            get
            {
                if (inventory?.Unique == null || inventory.EquippedId == null) return null;
                return inventory.Unique.TryGetValue(inventory.EquippedId.Value, out UniqueItemState item) ? item : null;
            }
        }

        public ConversationState ConversationWith(long userId)
        {
            if (talker?.Conversations == null) return null;
            return talker.Conversations.TryGetValue(userId, out ConversationState conversation) ? conversation : null;
        }

        public void Apply(EntityState state)
        {
            Position = new Vector3(state.X, state.Y, state.Z);
            Yaw = state.Yaw;

            Name = nameMapper.NameOf(state.Part<NameableState>());

            HealthState health = state.Part<HealthState>();
            Alive = health == null || health.Alive;
            Hp = health == null ? 0 : health.Hp;
            MaxHp = health == null ? 0 : health.MaxHp;

            SleeperState sleeper = state.Part<SleeperState>();
            Sleeping = sleeper != null && sleeper.Sleeping;

            PilotState pilot = state.Part<PilotState>();
            Piloted = pilot != null;
            UserId = pilot == null ? 0 : pilot.UserId;

            HandsState hands = state.Part<HandsState>();
            HandsAction = hands == null ? HandsAction.None : hands.Action;

            inventory = state.Part<InventoryState>();
            talker = state.Part<TalkerState>();

            OnApply(state.Part<SpeakerState>());
        }

        public virtual void Tick(float dt)
        {
        }

        public virtual void Destroy()
        {
        }

        protected abstract void OnApply(SpeakerState speakerState);
    }
}
