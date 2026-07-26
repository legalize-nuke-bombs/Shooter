using System.Collections.Generic;
using System.Threading.Tasks;
using Shooter.Configuring;

namespace Shooter.Game.Llm
{
    public interface ILlmProvider
    {
        Task<LlmAnswer> Request(LlmConfig config, string systemPrompt, IReadOnlyList<LlmMessage> messages);
    }
}
