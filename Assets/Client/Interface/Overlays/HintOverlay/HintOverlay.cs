using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Shooter.Client.Interface;
using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Logging;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    [RequireComponent(typeof(Aimer))]
    public class HintOverlay : MonoBehaviour
    {
        private const string HintElement = "hint";
        private const string KeyboardScheme = "Keyboard&Mouse";

        [SerializeField] private HintCatalog hints;

        private PanelRenderer panel;
        private Aimer aimer;
        private Label hint;
        private string key = string.Empty;
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
            hint = null;
        }

        private void Update()
        {
            if (hint == null) return;

            string text = Text();
            if (text == shown) return;

            shown = text;
            hint.text = text;
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            hint = root.Q<Label>(HintElement);
            shown = string.Empty;

            if (hint == null)
            {
                Log.Error("Overlay document has no {} label, interaction hints stay hidden", HintElement);
                return;
            }

            if (hints == null)
            {
                Log.Error("Hint overlay has no hint catalog, interaction hints stay hidden");
                hint = null;
                return;
            }

            key = Key();
            hint.text = string.Empty;
            Log.Info("Interaction hints are bound to key {}", key);
        }

        private string Text()
        {
            Interactor interactor = OwnPlayer.Find<Interactor>();
            if (interactor == null) return string.Empty;

            if (!aimer.TryHit(out RaycastHit hit) || hit.distance > interactor.Reach) return string.Empty;
            if (hit.collider == null) return string.Empty;

            var usable = hit.collider.GetComponentInParent<IUsable>();
            if (usable == null) return string.Empty;

            return "[" + key + "] " + hints.Text(usable.Usage);
        }

        private static string Key()
        {
            var controls = new Controls();
            string display = controls.Player.Interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardScheme));
            controls.Dispose();

            return display;
        }
    }
}
