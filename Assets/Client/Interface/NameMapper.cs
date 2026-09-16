using Shooter.Game.Body;
using Shooter.Game.Core;

namespace Shooter.Client.Interface
{
    public sealed class NameMapper
    {
        // The name the player sees for a character, empty when it is not around or has none
        public string Of(long characterId)
        {
            Character character = Character.Of(characterId, Inactive.Include);
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
                    return typed.Spec == null ? string.Empty : typed.Spec.Text();
                default:
                    return string.Empty;
            }
        }
    }
}
