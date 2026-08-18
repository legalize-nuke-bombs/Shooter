using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public interface ISaveableComponent
    {
        string ComponentKey { get; }

        object SaveComponent();

        void LoadComponent(JToken content);
    }
}
