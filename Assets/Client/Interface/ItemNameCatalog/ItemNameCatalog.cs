using System;
using System.Collections.Generic;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Client.Interface
{
    [CreateAssetMenu(menuName = "Shooter/Item Name Catalog", fileName = "ItemNameCatalog")]
    public sealed class ItemNameCatalog : ScriptableObject
    {
        [SerializeField] private ItemName[] names;

        private readonly HashSet<ItemType> unnamed = new HashSet<ItemType>();

        public string Text(ItemType type)
        {
            foreach (ItemName name in names)
            {
                if (name.Type == type) return name.Text;
            }

            if (unnamed.Add(type)) Log.Warn("Item name catalog {} has no name for {}", base.name, type);

            return type.ToString();
        }

        [Serializable]
        private struct ItemName
        {
            public ItemType Type;
            public string Text;
        }
    }
}
