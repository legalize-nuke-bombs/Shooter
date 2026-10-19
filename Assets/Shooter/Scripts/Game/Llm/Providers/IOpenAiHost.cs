using System.Threading;
using System.Threading.Tasks;

namespace Shooter.Game
{
    public interface IOpenAiHost
    {
        Task<string> RequestChatCompletion(string apiKey, OpenAiRequest body, CancellationToken cancellationToken);
    }
}
