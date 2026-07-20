using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class Entity
    {
        public long Id { get; }
        public GameObject Body { get; }

        private readonly Dictionary<Type, Part> parts = new Dictionary<Type, Part>();

        public Entity(long id, GameObject body)
        {
            Id = id;
            Body = body;
        }

        public void Add(Part part)
        {
            parts[part.GetType()] = part;
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
    }
}
