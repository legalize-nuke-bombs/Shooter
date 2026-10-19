using Newtonsoft.Json.Linq;

namespace Shooter.Game
{
    public interface ILlmTool
    {
        string Name { get; }
        string Description { get; }
        JObject Parameters { get; }

        string Execute(string arguments, LlmCallContext context);
    }
}
