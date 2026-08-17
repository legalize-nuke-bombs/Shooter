using System.Text;
using Shooter.Logging;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Game.Core
{
    public abstract class Spec : ScriptableObject
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string id;

        public string Key => string.IsNullOrEmpty(id) ? name : id;

        public FixedString32Bytes Id => new(Key);

        private void OnValidate()
        {
            if (!Fits())
                Log.Error(
                    $"{name} has an id of {Encoding.UTF8.GetByteCount(Key)} bytes, longer than the {FixedString32Bytes.UTF8MaxLengthInBytes} the network format holds");
        }

        public bool Fits()
        {
            return Encoding.UTF8.GetByteCount(Key) <= FixedString32Bytes.UTF8MaxLengthInBytes;
        }
    }
}
