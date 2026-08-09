using System;

namespace Shooter.Game.Base
{
    public class LlmProviderProfiler : BaseProfiler
    {
        private long sessionRequests = 0;

        private long sessionCharsIn = 0;
        private long sessionCharsOut = 0;

        private long sessionTokensIn = 0;
        private long sessionTokensOut = 0;

        public void RegisterSessionRequest(long? charsIn, long? charsOut, long? tokensIn, long? tokensOut)
        {
            charsIn ??= 0;
            charsOut ??= 0;
            tokensIn ??= 0;
            tokensOut ??= 0;

            if (charsIn < 0 || charsOut < 0 || tokensIn < 0 || tokensOut < 0)
            {
                throw new ArgumentException("arguments must be non negative");
            }

            sessionRequests++;

            sessionCharsIn += charsIn.Value;
            sessionCharsOut += charsOut.Value;

            sessionTokensIn += tokensIn.Value;
            sessionTokensOut += tokensOut.Value;
        }

        public override string LogLine()
        {
            return $"Session totals: {sessionRequests} requests, input {sessionCharsIn} chars / {sessionTokensIn} tokens, output {sessionCharsOut} chars / {sessionTokensOut} tokens";
        }
    }
}
