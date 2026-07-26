using UnityEngine;
using Shooter.Game.Naming;

namespace Shooter.Client.Naming
{
    public abstract class NameSpec : ScriptableObject
    {
        [SerializeField] private NameableType type;

        public NameableType Type => type;

        public abstract string Text();
    }
}
