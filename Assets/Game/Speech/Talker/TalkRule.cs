namespace Shooter.Game.Talking
{
    public static class TalkRule
    {
        public static bool CanTalk(bool speakerAlive, bool targetAlive, bool targetAwake)
        {
            return speakerAlive && targetAlive && targetAwake;
        }
    }
}
