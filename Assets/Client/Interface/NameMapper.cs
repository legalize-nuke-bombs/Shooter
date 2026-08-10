using Shooter.Game.Body;

namespace Shooter.Client.Interface.Naming
{
    public sealed class NameMapper
    {
        public string Of(Nameable nameable)
        {
            switch (nameable)
            {
                case AbsoluteNameable absolute:
                    return absolute.Name;
                case TypedNameable typed:
                    return typed.Spec == null ? string.Empty : typed.Spec.Text();
                default:
                    return string.Empty;
            }
        }
    }
}
