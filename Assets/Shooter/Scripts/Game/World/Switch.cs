using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Speaker))]
    public class Switch : NetworkBehaviour, IUsable, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] private bool lit = true;

        [SerializeField] private GameObject[] powered;

        [SerializeField] private Renderer[] glowing;

        [SerializeField] private SoundAsset click;

        private readonly NetworkVariable<bool> shining = new(true);

        private Color[] litEmissiveColors;
        private Speaker speaker;

        public string ComponentKey => "Switch";

        private struct SaveData
        {
            public bool Shining { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Shining = shining.Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            shining.Value = saveData.Shining;
        }

        private void Awake()
        {
            speaker = GetComponent<Speaker>();
            litEmissiveColors = new Color[glowing.Length];

            for (int i = 0; i < glowing.Length; i++)
            {
                litEmissiveColors[i] = glowing[i] == null ? Color.black : glowing[i].material.GetColor(EmissiveColorId);
            }
        }

        public string Digest(DigestionDetail detail)
        {
            return shining.Value ? "Turned on" : "Turned off";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public UsageType Usage => shining.Value ? UsageType.TurnOff : UsageType.TurnOn;

        public void Use(NetworkObject user, out string promptResult)
        {
            shining.Value = !shining.Value;
            speaker.PlayForEveryone(click);
            Log.Info($"Entity {user.name} switched {name} {(shining.Value ? "on" : "off")}");
            promptResult = $"Switched to {shining.Value}";
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) shining.Value = lit;

            shining.OnValueChanged += HandleShiningChanged;
            SwitchPoweredAndGlowing(shining.Value);
        }

        public override void OnNetworkDespawn()
        {
            shining.OnValueChanged -= HandleShiningChanged;
        }

        private void HandleShiningChanged(bool wasShining, bool isShining)
        {
            SwitchPoweredAndGlowing(isShining);
        }

        private void SwitchPoweredAndGlowing(bool isShining)
        {
            foreach (GameObject poweredObject in powered)
            {
                if (poweredObject != null) poweredObject.SetActive(isShining);
            }

            for (int i = 0; i < glowing.Length; i++)
            {
                if (glowing[i] != null) glowing[i].material.SetColor(EmissiveColorId, isShining ? litEmissiveColors[i] : Color.black);
            }
        }
    }
}
