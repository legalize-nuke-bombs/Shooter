using Shooter.Client.Interface.Naming;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    [RequireComponent(typeof(Aimer))]
    public class TargetNameOverlay : MonoBehaviour
    {
        private const string TargetElement = "target-name";

        [SerializeField] private NameCatalog names;

        private PanelRenderer panel;
        private Aimer aimer;
        private NameMapper mapper;
        private Label target;
        private string shown = string.Empty;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            aimer = GetComponent<Aimer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);
            target = null;
        }

        private void Update()
        {
            if (target == null) return;

            Nameable nameable = Aimed();
            string text = nameable == null ? string.Empty : mapper.Of(nameable);

            if (text == shown) return;

            shown = text;
            target.text = text;
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            target = root.Q<Label>(TargetElement);
            shown = string.Empty;

            if (target == null)
            {
                Log.Error("Overlay document has no {} label, target names stay hidden", TargetElement);
                return;
            }

            if (names == null)
            {
                Log.Error("Target name overlay has no name catalog, target names stay hidden");
                target = null;
                return;
            }

            mapper = new NameMapper(names);
            target.text = string.Empty;
        }

        private Nameable Aimed()
        {
            if (!aimer.TryHit(out RaycastHit hit)) return null;
            if (hit.collider == null) return null;

            return hit.collider.GetComponentInParent<Nameable>();
        }
    }
}
