using Shooter.Client.Interface.Naming;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(Aimer))]
    public class TargetNameOverlay : Overlay
    {
        private const string TargetElement = "target-name";

        [SerializeField] private NameCatalog names;

        private Aimer aimer;
        private NameMapper mapper;
        private Label target;
        private string shown = string.Empty;

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
                Log.Error("Overlay document has no {} label, target names stay hidden", TargetElement);
                return false;
            }

            if (names == null)
            {
                Log.Error("Target name overlay has no name catalog, target names stay hidden");
                return false;
            }

            mapper = new NameMapper(names);
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
