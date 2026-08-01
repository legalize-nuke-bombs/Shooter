using Shooter.Game.Body;
using Shooter.Game.Body.Sounding;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Speaker))]
    public class Switch : NetworkBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly int Emissive = Shader.PropertyToID("_EmissiveColor");

        [SerializeField] private bool lit = true;

        [SerializeField] private GameObject[] powered;

        [SerializeField] private Renderer[] glowing;

        [SerializeField] private SoundSpec click;

        private readonly NetworkVariable<bool> shining = new NetworkVariable<bool>(true);

        private Color[] glows;

        private Speaker speaker;

        public UsageType Usage => shining.Value ? UsageType.TurnOff : UsageType.TurnOn;

        private void Awake()
        {
            speaker = GetComponent<Speaker>();
            glows = new Color[glowing.Length];

            for (int i = 0; i < glowing.Length; i++)
                glows[i] = glowing[i] == null ? Color.black : glowing[i].material.GetColor(Emissive);
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

        public void Use(NetworkObject user)
        {
            shining.Value = !shining.Value;
            speaker.Play(click);
            Log.Info("Entity {} switched {} {}", user.name, name, shining.Value ? "on" : "off");
        }

        private void Switched(bool was, bool now)
        {
            Apply(now);
        }

        public string Digest(DigestionDetail detail)
        {
            return shining.Value ? "Включен" : "Выключен";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        private void Apply(bool now)
        {
            foreach (GameObject part in powered)
            {
                if (part != null) part.SetActive(now);
            }

            for (int i = 0; i < glowing.Length; i++)
            {
                if (glowing[i] != null) glowing[i].material.SetColor(Emissive, now ? glows[i] : Color.black);
            }
        }
    }
}
