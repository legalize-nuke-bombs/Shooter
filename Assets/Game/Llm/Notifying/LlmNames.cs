using Shooter.Game.Notifying;
using Shooter.Game.Loot;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm
{
    public class LlmNames : INames
    {
        public string Of(string name, string value)
        {
            if (name == "actor") return $"Character {value}";
            if (name == "subject") return Prompted(value);

            return value;
        }

        private static string Prompted(string value)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;
            ItemSpec spec = catalog == null ? null : catalog.Spec(value);

            return spec == null ? value : spec.PromptName;
        }
    }
}
