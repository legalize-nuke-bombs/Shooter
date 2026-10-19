using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class NameMapper
    {
        public string GetShownNameById(long characterId)
        {
            Character character = Character.FindById(characterId, RegisterInactivePolicy.Include);
            Nameable nameable = character == null ? null : character.GetComponentInChildren<Nameable>();

            return nameable == null ? string.Empty : GetShownName(nameable);
        }

        public string GetShownName(Nameable nameable)
        {
            switch (nameable)
            {
                case AbsoluteNameable absoluteNameable:
                    return absoluteNameable.Name;
                case TypedNameable typedNameable:
                    return typedNameable.Asset == null ? string.Empty : typedNameable.Asset.GetShownName();
                default:
                    return string.Empty;
            }
        }
    }
}
