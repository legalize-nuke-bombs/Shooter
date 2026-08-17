using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    [RequireComponent(typeof(Aimer))]
    public class TargetNameOverlay : Overlay
    {
        private const string TargetElement = "target-name";
        private static readonly Journal Log = Logs.Here();

        private readonly NameMapper mapper = new();

        private Aimer aimer;
        private string shown = string.Empty;
        private Label target;

        private void Awake()
        {
            aimer = GetComponent<Aimer>();
        }

        private void Update()
        {
            if (!Bound) return;

            Nameable nameable = Aimed();
            string text = nameable == null ? string.Empty : mapper.Of(nameable);

            if (text == shown) return;

            shown = text;
            target.text = text;
        }

        protected override bool Bind(VisualElement root)
        {
            target = root.Q<Label>(TargetElement);
            shown = string.Empty;

            if (target == null)
            {
                Log.Error($"Overlay document has no {TargetElement} label, target names stay hidden");
                return false;
            }

            target.text = string.Empty;

            return true;
        }

        protected override void Unbind()
        {
            target = null;
        }

        private Nameable Aimed()
        {
            if (!aimer.TryHit(out RaycastHit hit)) return null;
            if (hit.collider == null) return null;

            return hit.collider.GetComponentInParent<Nameable>();
        }
    }
}
