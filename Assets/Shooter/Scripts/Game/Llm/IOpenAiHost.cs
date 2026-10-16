using System.Threading;
using System.Threading.Tasks;

namespace Shooter.Game
{
    public interface IOpenAiHost
    {
        Task<string> Request(string key, OpenAiRequest body, CancellationToken until);
    }
}
