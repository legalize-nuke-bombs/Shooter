using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(Aimer))]
    public class HintOverlay : Overlay
    {
        private const string HintElement = "hint";
        private const string KeyboardScheme = "Keyboard&Mouse";

        [SerializeField] private HintCatalog hints;

        private Aimer aimer;
        private Label hint;
        private string key = string.Empty;
        private string shown = string.Empty;

        private void Awake()
        {
            aimer = GetComponent<Aimer>();
        }

        private void Update()
        {
            if (!Bound) return;

            string text = Text();
            if (text == shown) return;

            shown = text;
            hint.text = text;
        }

        protected override bool Bind(VisualElement root)
        {
            hint = root.Q<Label>(HintElement);
            shown = string.Empty;

            if (hint == null)
            {
                Log.Error("Overlay document has no {} label, interaction hints stay hidden", HintElement);
                return false;
            }

            if (hints == null)
            {
                Log.Error("Hint overlay has no hint catalog, interaction hints stay hidden");
                return false;
            }

            key = Key();
            hint.text = string.Empty;
            Log.Info("Interaction hints are bound to key {}", key);

            return true;
        }

        protected override void Unbind()
        {
            hint = null;
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
