using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(NetworkTransform))]
    public class SaveableTransform : NetworkBehaviour, ISaveableComponent
    {
        public string ComponentKey => "SaveableTransform";
        private struct SaveData
        {
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }
            public Vector3 Scale { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData data = content.To<SaveData>();
            GetComponent<NetworkTransform>().Teleport(data.Position, data.Rotation, data.Scale);
        }
    }
}
