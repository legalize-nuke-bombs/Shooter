using UnityEngine;
using System.Linq;

namespace Shooter.Server.Entities.Npcs.Specs.Nameable
{
    public class CorruptedNameable : INameable
    {
        public string Name()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[Random.Range(0, s.Length)]).ToArray());
        }
    }
}
