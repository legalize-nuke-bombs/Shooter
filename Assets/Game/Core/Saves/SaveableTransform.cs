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
            public float[] Position { get; set; }
            public float[] Rotation { get; set; }
            public float[] Scale { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveDto
            {
                Position = new[]{transform.position.x, transform.position.y, transform.position.z},
                Rotation = new[]{transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w},
                Scale = new[]{transform.localScale.x, transform.localScale.y, transform.localScale.z}
            };
        }
        public void LoadComponent(JToken content)
        {
            SaveDto sd = content.ToObject<SaveDto>();

            var position = new Vector3(sd.Position[0], sd.Position[1], sd.Position[2]);
            var rotation = new Quaternion(sd.Rotation[0], sd.Rotation[1], sd.Rotation[2], sd.Rotation[3]);
            var scale = new Vector3(sd.Scale[0], sd.Scale[1], sd.Scale[2]);

            GetComponent<NetworkTransform>().Teleport(position, rotation, scale);
        }
    }
}
