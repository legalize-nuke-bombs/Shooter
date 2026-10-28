using System.Collections.Generic;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public interface ILlmFormat
    {
        object BuildRequestBody(LlmConfig config, string systemPrompt, IReadOnlyList<LlmMessage> history, IReadOnlyList<ILlmTool> tools);

        LlmMessage ParseReply(string responseJson);
    }
}
