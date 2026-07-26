using System.Text;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Item", fileName = "Item")]
    public class ItemSpec : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string promptName;
        [SerializeField] private bool stackable;
        [SerializeField] private bool equipable;

        public string Key => string.IsNullOrEmpty(id) ? name : id;

        public FixedString32Bytes Id => new FixedString32Bytes(Key);

        public string Title => string.IsNullOrEmpty(title) ? Key : title;

        public string PromptName => string.IsNullOrEmpty(promptName) ? Key : promptName;

        public bool Stackable => stackable;

        public bool Equipable => equipable;

        public bool Fits()
        {
            return Encoding.UTF8.GetByteCount(Key) <= FixedString32Bytes.UTF8MaxLengthInBytes;
        }

        private void OnValidate()
        {
            if (!Fits())
                Log.Error("Item {} has an id of {} bytes, longer than the {} the network format holds",
                    name, Encoding.UTF8.GetByteCount(Key), FixedString32Bytes.UTF8MaxLengthInBytes);
        }
    }
}
