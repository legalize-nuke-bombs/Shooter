using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities.Parts;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class Entity
    {
        public Guid Id { get; }

        private readonly GameObject body;
        private readonly Dictionary<Type, Part> slots = new Dictionary<Type, Part>();
        private readonly List<Part> parts = new List<Part>();

        public Entity(string kind, Vector3 position)
        {
            Id = Guid.NewGuid();
            body = new GameObject(kind + "_" + Id);
            body.transform.position = position;
            ServerEntityBody.Bind(body, Id);
        }

        public string Name => body.name;

        public Vector3 Position => body.transform.position;

        public float Yaw => body.transform.eulerAngles.y;

        public T Attach<T>() where T : Component
        {
            return body.AddComponent<T>();
        }

        public void MoveToScene(Scene scene)
        {
            SceneManager.MoveGameObjectToScene(body, scene);
        }

        public void Add(Part part)
        {
            if (part.Self != this)
            {
                Log.Error("Part {} of entity {} can not be added to entity {}", part.GetType().Name, part.Self.Id, Id);
                return;
            }

            if (slots.TryGetValue(part.Slot, out Part occupant))
            {
                Log.Warn("Part {} of entity {} replaces {} occupying the same slot", part.GetType().Name, Id, occupant.GetType().Name);
                parts.Remove(occupant);
            }

            slots[part.Slot] = part;
            parts.Add(part);
        }

        public T Get<T>() where T : Part
        {
            return slots.TryGetValue(typeof(T), out Part part) ? (T)part : null;
        }

        public bool Has<T>() where T : Part
        {
            return slots.ContainsKey(typeof(T));
        }

        public void Apply(PlayerIntent input)
        {
            foreach (Part part in parts)
                part.Apply(input);
        }

        public void Tick(float dt)
        {
            foreach (Part part in parts)
                part.Tick(dt);
        }

        public void Died()
        {
            Log.Info("Entity {} died", Name);
            foreach (Part part in parts)
                part.Died();
        }

        public string Digest()
        {
            var lines = new List<string>();
            foreach (Part part in parts)
            {
                string line = part.Digest();
                if (!string.IsNullOrEmpty(line)) lines.Add(line);
            }
            return string.Join("\n", lines);
        }

        public EntityState State()
        {
            var states = new List<PartState>();
            foreach (Part part in parts)
            {
                PartState state = part.State();
                if (state != null) states.Add(state);
            }

            Vector3 position = Position;
            return new EntityState
            {
                Id = Id,
                X = position.x,
                Y = position.y,
                Z = position.z,
                Yaw = Yaw,
                Parts = states
            };
        }

        public void Destroy()
        {
            if (body != null) UnityEngine.Object.Destroy(body);
        }
    }
}
