using Newtonsoft.Json;

namespace Shooter.Game.Loot
{
    public class UniqueItem
    {
        public string SpecId { get; private set; }

        [JsonIgnore] public bool Dirty { get; private set; }

        public UniqueItem(string specId)
        {
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
