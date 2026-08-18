using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(StructureHealth))]
    [RequireComponent(typeof(Speaker))]
    public class Switch : NetworkBehaviour, IUsable, IDigestible, IBreakable
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] private bool lit = true;

        [SerializeField] private GameObject[] powered;

        [SerializeField] private Renderer[] glowing;

        [SerializeField] private SoundSpec click;

        private readonly NetworkVariable<bool> shining = new(true);

        private Color[] glows;
        private Speaker speaker;

        private StructureHealth structureHealth;

        private void Awake()
        {
            structureHealth = GetComponent<StructureHealth>();
            speaker = GetComponent<Speaker>();
            glows = new Color[glowing.Length];

            for (int i = 0; i < glowing.Length; i++)
                glows[i] = glowing[i] == null ? Color.black : glowing[i].material.GetColor(Emissive);
        }

        public void Broken()
        {
            Log.Info($"Entity {name} will be switched off because it was broken");
            shining.Value = false;
        }

        public string Digest(DigestionDetail detail)
        {
            return shining.Value
                ? "Turned on"
                : structureHealth.Broken
                    ? "Broken"
                    : "Turned off";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public UsageType Usage => shining.Value ? UsageType.TurnOff : UsageType.TurnOn;

        public void Use(NetworkObject user)
        {
            if (structureHealth.Broken)
            {
                Log.Info($"Entity {user.name} tried to switch {name} but it was broken");
                return;
            }

            shining.Value = !shining.Value;
            speaker.Play(click);
            Log.Info($"Entity {user.name} switched {name} {(shining.Value ? "on" : "off")}");
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) shining.Value = lit;

            shining.OnValueChanged += Switched;
            Apply(shining.Value);
        }

        public override void OnNetworkDespawn()
        {
            shining.OnValueChanged -= Switched;
        }

        private void Switched(bool was, bool now)
        {
            Apply(now);
        }

        private void Apply(bool now)
        {
            foreach (GameObject part in powered)
                if (part != null)
                    part.SetActive(now);

            for (int i = 0; i < glowing.Length; i++)
                if (glowing[i] != null)
                    glowing[i].material.SetColor(Emissive, now ? glows[i] : Color.black);
        }
    }
}
