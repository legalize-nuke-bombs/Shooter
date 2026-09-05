namespace Shooter.Game.Speech
{
    public static class TalkRule
    {
        public static bool CanTalk(bool speakerAlive, bool speakerAwake, bool listenerAlive, bool listenerAwake)
        {
            return speakerAlive && speakerAwake && listenerAlive && listenerAwake;
        }
    }
}
