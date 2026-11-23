using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public interface ILlmHost
    {
        ILlmFormat Format { get; }

        IReadOnlyCollection<string> Settings { get; }

        Task<string> Send(LlmConfig config, string requestJson, CancellationToken cancellationToken);
    }
}
