using Shooter.Game.Body.Notifying;
using Shooter.Game.Loot;

namespace Shooter.Game.Llm.Notifying
{
    public class LlmNames : INames
    {
        public string Of(string name, string value)
        {
            if (name == Args.Actor) return $"Character {value}";
            if (name == Args.Subject) return Prompted(value);

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
