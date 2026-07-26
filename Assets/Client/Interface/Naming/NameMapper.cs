using Shooter.Game.Naming;

namespace Shooter.Client.Naming
{
    public sealed class NameMapper
    {
        private readonly NameCatalog catalog;

        public NameMapper(NameCatalog catalog)
        {
            this.catalog = catalog;
        }

        public string Of(Nameable nameable)
        {
            switch (nameable)
            {
                case AbsoluteNameable absolute:
                    return absolute.Name;
                case TypedNameable typed:
                    return catalog.Text(typed.Type);
                default:
                    return string.Empty;
            }
        }
    }
}
