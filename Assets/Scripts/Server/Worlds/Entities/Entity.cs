using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class Entity
    {
        public Guid Id { get; }
        public GameObject Body { get; }

        private readonly Dictionary<Type, Part> parts = new Dictionary<Type, Part>();

        public Entity(Guid id, GameObject body)
        {
            Id = id;
            Body = body;
        }

        public void Add(Part part)
        {
            parts[part.Slot] = part;
        }

        public T Get<T>() where T : Part
        {
            return parts.TryGetValue(typeof(T), out Part part) ? (T)part : null;
        }

        public bool Has<T>() where T : Part
        {
            return parts.ContainsKey(typeof(T));
        }

        public void Tick(float dt)
        {
            foreach (Part part in parts.Values)
                part.Tick(this, dt);
        }

        public EntityState State()
        {
            var states = new List<PartState>();
            foreach (Part part in parts.Values)
            {
                PartState state = part.State();
                if (state != null) states.Add(state);
            }

            Vector3 position = Body.transform.position;
            return new EntityState
            {
                Id = Id,
                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = Body.transform.eulerAngles.y,
                Parts = states
            };
        }

        public void Destroy()
        {
            if (Body != null) UnityEngine.Object.Destroy(Body);
        }
    }
}
