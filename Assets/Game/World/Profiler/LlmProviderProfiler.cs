namespace Shooter.Game.World
{
    public class LlmProviderProfiler : BaseProfiler
    {
        private long sessionRequests = 0;

        private long sessionCharsIn = 0;
        private long sessionCharsOut = 0;

        private long sessionTokensIn = 0;
        private long sessionTokensOut = 0;

        public void RegisterSessionRequest(long charsIn, long charsOut, long? tokensIn, long? tokensOut)
        {
            sessionRequests++;

            sessionCharsIn += charsIn;
            sessionCharsOut += charsOut;

            sessionTokensIn += tokensIn ?? 0;
            sessionTokensOut += tokensOut ?? 0;
        }

        public override string LogLine()
        {
            if (sessionRequests == 0)
            {
                return null;
            }

            return $"Session totals: {sessionRequests} requests, input {sessionCharsIn} chars / {sessionTokensIn} tokens, output {sessionCharsOut} chars / {sessionTokensOut} tokens";
        }
    }
}
