using System;
using System.Collections.Generic;
using Shooter.Game.Core.Saves;
using Shooter.Logging;

namespace Shooter.Game.AI.Bt.CustomOrders
{
    public abstract class BtCustomOrder : ISaveable
    {
        private static readonly Journal Log = Logs.Here();
        private static Dictionary<string, Type> kinds;

        public BtCustomOrderStatus Status { get; private set; }

        public abstract string Kind { get; }
        public abstract object SaveObject();
        public abstract void LoadObject(SaveToken content);

        public void Begin()
        {
            Status = BtCustomOrderStatus.Running;
        }

        public void Suspend()
        {
            Status = BtCustomOrderStatus.Pending;
        }

        protected abstract string PromptRawDescription();
        public string PromptDescription()
        {
            return $"[{Status.ToString()}] " + PromptRawDescription();
        }

        public static BtCustomOrder Create(string kind)
        {
            kinds ??= Discover();
            return kinds.TryGetValue(kind, out Type type) ? (BtCustomOrder)Activator.CreateInstance(type) : null;
        }

        private static Dictionary<string, Type> Discover()
        {
            var found = new Dictionary<string, Type>();

            foreach (Type type in typeof(BtCustomOrder).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(BtCustomOrder).IsAssignableFrom(type)) continue;

                var sample = (BtCustomOrder)Activator.CreateInstance(type);
                if (!found.TryAdd(sample.Kind, type))
                    Log.Error($"Order kind {sample.Kind} is claimed by both {found[sample.Kind].Name} and {type.Name}");
            }

            return found;
        }
    }
}
