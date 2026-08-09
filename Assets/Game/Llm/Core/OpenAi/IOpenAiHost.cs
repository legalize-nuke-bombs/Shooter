using System.Threading;
using System.Threading.Tasks;

namespace Shooter.Game.Llm.OpenAi
{
    public interface IOpenAiHost
    {
        Task<string> Request(string key, OpenAiRequest body, CancellationToken until);
    }
}
