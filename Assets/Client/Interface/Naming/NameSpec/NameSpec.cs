using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Client.Interface.Naming
{
    public abstract class NameSpec : ScriptableObject
    {
        [SerializeField] private NameableType type;

        public NameableType Type => type;

        public abstract string Text();
    }
}
