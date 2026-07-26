namespace Shooter.Game.Speech
{
    public static class TalkRule
    {
        public static bool CanTalk(bool speakerAlive, bool targetAlive, bool targetAwake)
        {
            return speakerAlive && targetAlive && targetAwake;
        }
    }
}
