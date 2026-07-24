namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public static class TalkRule
    {
        public static bool CanTalk(bool speakerAlive, bool targetAlive, bool targetTalkative)
        {
            return speakerAlive && targetAlive && targetTalkative;
        }
    }
}
