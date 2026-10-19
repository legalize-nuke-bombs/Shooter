using System.Text;
using Unity.Collections;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public abstract class Asset : ScriptableObject
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string id;

        public string Id => string.IsNullOrEmpty(id) ? name : id;

        public FixedString32Bytes NetworkId => new(Id);

        private void OnValidate()
        {
            if (!IdFitsNetwork())
            {
                Log.Error($"{name} has an id of {Encoding.UTF8.GetByteCount(Id)} bytes, longer than the {FixedString32Bytes.UTF8MaxLengthInBytes} the network format holds");
            }
        }

        public bool IdFitsNetwork()
        {
            return Encoding.UTF8.GetByteCount(Id) <= FixedString32Bytes.UTF8MaxLengthInBytes;
        }
    }
}
