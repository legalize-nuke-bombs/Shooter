using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Health;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Movement;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using Shooter.Server.Worlds.Entities.Parts.Talker;
using Shooter.Server.Worlds.Entities.Parts.Talker.Gemini;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Creating
{
    public static class NpcCreator
    {
        private const int NpcHp = 100;

        private const string KapsulCharacter =
            "Тебя зовут Капсул. Ты первый NPC добавленный в игру. Ты дружелюбный и эмпатичный. Ты помогаешь игроку.";

        public static Entity Kapsul(Vector3 at, Clock clock)
        {
            Entity npc = Npc("Kapsul", at, NameableType.Kapsul);
            npc.Add(new GeminiTalker(npc, clock, KapsulCharacter, GeminiModel.Flash35));
            return npc;
        }

        public static Entity Corrupted(Vector3 at)
        {
            Entity npc = Npc("Corrupted", at, NameableType.SpecialCorrupted);
            npc.Add(new RefusiveTalker(npc));
            return npc;
        }

        private static Entity Npc(string kind, Vector3 at, NameableType nameableType)
        {
            Log.Info("Creating npc {} at {}...", kind, at);

            var npc = new Entity(kind, at);
            npc.Add(new Movement(npc));
            npc.Add(new Nameable(npc, nameableType));
            npc.Add(new DefaultHealth(npc, NpcHp));
            npc.Add(new Inventory(npc));
            return npc;
        }
    }
}
