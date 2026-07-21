using System;
using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Parts;

namespace Shooter.Server.Worlds.Entities
{
    public class EntityState
    {
        public Guid Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public List<PartState> Parts { get; set; }

        public T Part<T>() where T : PartState
        {
            if (Parts == null) return null;
            foreach (PartState part in Parts)
                if (part is T typed) return typed;
            return null;
        }
    }
}
