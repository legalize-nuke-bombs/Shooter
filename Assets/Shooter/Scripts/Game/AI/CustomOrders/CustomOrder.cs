using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class CustomOrder : ISaveable
    {
        private static readonly Journal Log = Logs.Here();
        private static Dictionary<string, Type> orderTypesByKind;

        public CustomOrderStatus Status { get; private set; }

        public abstract string Kind { get; }
        public abstract object SaveObject();
        public abstract void LoadObject(SaveToken content);

        public abstract bool IsDone(GameObject body);
        public abstract string GetPromptOutcome(GameObject body);

        public void MarkRunning()
        {
            Status = CustomOrderStatus.Running;
        }

        public void MarkPending()
        {
            Status = CustomOrderStatus.Pending;
        }

        protected abstract string GetPromptDescriptionWithoutStatus(GameObject body);
        public string GetPromptDescription(GameObject body)
        {
            return $"[{Status.ToString()}] " + GetPromptDescriptionWithoutStatus(body);
        }

        public static CustomOrder CreateOfKind(string kind)
        {
            orderTypesByKind ??= FindOrderTypesByKind();
            return orderTypesByKind.TryGetValue(kind, out Type type) ? (CustomOrder)Activator.CreateInstance(type) : null;
        }

        private static Dictionary<string, Type> FindOrderTypesByKind()
        {
            var typesByKind = new Dictionary<string, Type>();

            foreach (Type type in typeof(CustomOrder).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CustomOrder).IsAssignableFrom(type)) continue;

                CustomOrder sample = (CustomOrder)Activator.CreateInstance(type);
                if (!typesByKind.TryAdd(sample.Kind, type))
                {
                    Log.Error($"Order kind {sample.Kind} is claimed by both {typesByKind[sample.Kind].Name} and {type.Name}");
                }
            }

            return typesByKind;
        }
    }
}
