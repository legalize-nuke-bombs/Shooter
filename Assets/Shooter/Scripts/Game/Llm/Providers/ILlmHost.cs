using System.Threading;
using System.Threading.Tasks;

namespace Shooter.Game
{
    public interface ILlmHost
    {
        ILlmFormat Format { get; }

        Task<string> Send(string apiKey, string requestJson, CancellationToken cancellationToken);
    }
}
