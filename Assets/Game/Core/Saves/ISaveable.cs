using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public interface ISaveable
    {
        object SaveObject();

        void LoadObject(JToken content);
    }
}
