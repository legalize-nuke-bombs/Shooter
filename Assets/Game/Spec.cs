using System.Text;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Spec : ScriptableObject
    {
        [SerializeField] private string id;

        public string Key => string.IsNullOrEmpty(id) ? name : id;

        public FixedString32Bytes Id => new FixedString32Bytes(Key);

        public bool Fits()
        {
            return Encoding.UTF8.GetByteCount(Key) <= FixedString32Bytes.UTF8MaxLengthInBytes;
        }

        private void OnValidate()
        {
            if (!Fits())
                Log.Error("{} has an id of {} bytes, longer than the {} the network format holds",
                    name, Encoding.UTF8.GetByteCount(Key), FixedString32Bytes.UTF8MaxLengthInBytes);
        }
    }
}
