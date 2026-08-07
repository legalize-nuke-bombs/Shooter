using Newtonsoft.Json;

namespace Shooter.Game.Loot
{
    public class UniqueItem
    {
        public ulong Id { get; private set; }

        public string SpecId { get; private set; }

        [JsonIgnore] public bool Dirty { get; private set; }

        public UniqueItem(ulong id, string specId)
        {
            Id = id;
            SpecId = specId;
        }

        public void Clean()
        {
            Dirty = false;
        }

        protected void Touch()
        {
            Dirty = true;
        }
    }
}
