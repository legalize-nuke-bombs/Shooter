using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
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
