using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class NameMapper
    {
        public string GetShownNameById(long characterId)
        {
            Character character = Character.FindById(characterId, RegisterInactivePolicy.Include);
            Nameable nameable = character == null ? null : character.GetComponentInChildren<Nameable>();

            return nameable == null ? string.Empty : nameable.GetShownName();
        }
    }
}
