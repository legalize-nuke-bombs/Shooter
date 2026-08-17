using UnityEngine;

namespace Shooter.Game.AI
{
    public abstract class AISetting : MonoBehaviour
    {
        public abstract string Name { get; }
        public abstract string Range { get; }
        public abstract string Description { get; }
        public abstract void Set(string content);
        public abstract string Get();
    }
}
