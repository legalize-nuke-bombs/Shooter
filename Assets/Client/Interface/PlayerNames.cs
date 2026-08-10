using Shooter.Game.Body;
using Shooter.Game.Llm;
using Shooter.Game.Notifying;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Client.Interface
{
    public class PlayerNames : INames
    {
        private const string Stranger = "Незнакомец";

        private readonly NameMapper mapper = new NameMapper();

        public string Of(string name, string value)
        {
            if (name == Args.Actor) return Named(value);
            if (name == Args.Subject) return Titled(value);

            return value;
        }

        private string Named(string value)
        {
            if (!long.TryParse(value, out long id)) return value;

            PersistentId actor = Environment.Current == null ? null : Environment.Current.PersistentIds.Of(id);
            if (actor == null) return Stranger;

            var nameable = actor.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string named = mapper.Of(nameable);

            return string.IsNullOrEmpty(named) ? Stranger : named;
        }

        private static string Titled(string value)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;
            ItemSpec spec = catalog == null ? null : catalog.Spec(value);

            return spec == null ? value : spec.Title;
        }
    }
}
