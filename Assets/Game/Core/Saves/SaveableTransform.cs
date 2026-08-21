using Newtonsoft.Json.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(NetworkTransform))]
    public class SaveableTransform : NetworkBehaviour, ISaveableComponent
    {
        public string ComponentKey => "SaveableTransform";
        private struct SaveDto
        {
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }
            public Vector3 Scale { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveDto
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }
        public void LoadComponent(JToken content)
        {
            SaveDto sd = content.ToObject<SaveDto>();
            GetComponent<NetworkTransform>().Teleport(sd.Position, sd.Rotation, sd.Scale);
        }
    }
}
