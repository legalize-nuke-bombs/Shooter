using Shooter.Configuring;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(CustomPassVolume))]
    public class Vhs : MonoBehaviour
    {
        private static readonly int Strength = Shader.PropertyToID("_VhsStrength");

        private ClientConfig client;
        private CustomPassVolume volume;

        private void Awake()
        {
            volume = GetComponent<CustomPassVolume>();
        }

        private void OnEnable()
        {
            client = Config.Read().Client;
            client.propertyChanged += Changed;
            Apply();
        }

        private void OnDisable()
        {
            client.propertyChanged -= Changed;
        }

        private void Changed(object sender, BindablePropertyChangedEventArgs args)
        {
            Apply();
        }

        private void Apply()
        {
            Shader.SetGlobalFloat(Strength, client.Vhs);
            volume.enabled = client.Vhs > 0f;
        }
    }
}
