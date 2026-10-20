using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    // The name the player sees for a character, online or not; one without a name, or gone from the world, is a stranger
    public static class NameMapper
    {
        private const string Stranger = "Незнакомец";

        public static string GetShownNameById(long characterId)
        {
            return GetShownName(Character.FindById(characterId, RegisterInactivePolicy.Include));
        }

        public static string GetShownName(Component character)
        {
            Nameable nameable = character == null ? null : character.GetComponentInChildren<Nameable>(true);
            string shownName = nameable == null ? null : nameable.GetShownName();

            return string.IsNullOrEmpty(shownName) ? Stranger : shownName;
        }
    }
}
