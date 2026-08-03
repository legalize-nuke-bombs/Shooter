using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class TypedNameable : Nameable
    {
        [SerializeField] private NameSpec spec;

        public NameSpec Spec => spec;

        public void Assign(NameSpec assigned)
        {
            spec = assigned;
        }

        public override string Digest(DigestionDetail detail)
        {
            return spec == null ? null : spec.Prompt();
        }

        public override string PromptName()
        {
            return spec.Prompt();
        }
    }
}
