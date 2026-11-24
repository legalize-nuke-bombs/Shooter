using System.Collections.Generic;

namespace Shooter.Game
{
    public interface IOpenAiReasoning
    {
        IEnumerable<LlmReasoningPart> ReadFrom(OpenAiMessage message);

        void WriteTo(OpenAiMessage message, IEnumerable<LlmReasoningPart> reasoningParts);
    }
}
