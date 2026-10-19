using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class NameMapper
    {
        public string Of(long characterId)
        {
            Character character = Character.FindById(characterId, RegisterInactivePolicy.Include);
            Nameable nameable = character == null ? null : character.GetComponentInChildren<Nameable>();

            return nameable == null ? string.Empty : Of(nameable);
        }

        public string Of(Nameable nameable)
        {
            switch (nameable)
            {
                case AbsoluteNameable absolute:
                    return absolute.Name;
                case TypedNameable typed:
                    return typed.Asset == null ? string.Empty : typed.Asset.GetShownName();
                default:
                    return string.Empty;
            }
        }
    }
}
