using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register<T> where T : Component
    {
        private readonly Registers world;

        internal Register(Registers world)
        {
            this.world = world;
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (Component member in world.Members)
                    if (member is T)
                        count++;

                return count;
            }
        }

        public IEnumerable<T> All
        {
            get
            {
                foreach (Component member in world.Members)
                    if (member is T typed)
                        yield return typed;
            }
        }
    }
}
