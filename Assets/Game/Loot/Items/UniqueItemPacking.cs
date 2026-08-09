using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Logging;
using Unity.Collections;

namespace Shooter.Game.Loot
{
    public static class UniqueItemPacking
    {
        private static readonly Journal Log = Logs.Here();

        public static FixedString4096Bytes Pack(UniqueItem item)
        {
            string state = JsonConvert.SerializeObject(item);
            int size = Encoding.UTF8.GetByteCount(state);

            if (size <= FixedString4096Bytes.UTF8MaxLengthInBytes) return new FixedString4096Bytes(state);

            Log.Error($"Thing {item.SpecId} takes {size} bytes of state, more than the {FixedString4096Bytes.UTF8MaxLengthInBytes} the network format holds");

            return default;
        }

        public static UniqueItem Unpack(FixedString4096Bytes packed)
        {
            if (packed.IsEmpty) return null;

            string json = packed.ToString();
            JObject parsed = JObject.Parse(json);
            var specId = parsed.Value<string>(nameof(UniqueItem.SpecId));

            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;
            ItemSpec spec = catalog == null ? null : catalog.Spec(specId);

            if (spec == null)
            {
                Log.Error($"A thing of unknown kind {specId} arrived");
                return null;
            }

            UniqueItem item = spec.Create();
            JsonConvert.PopulateObject(json, item);
            item.Clean();

            return item;
        }
    }
}
