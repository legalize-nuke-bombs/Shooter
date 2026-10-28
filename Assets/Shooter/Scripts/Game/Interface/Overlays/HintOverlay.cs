using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LookHitFinder))]
    public class HintOverlay : Overlay, IShooterUpdateable
    {
        private const string HintLabelName = "hint";
        private const string KeyboardScheme = "Keyboard&Mouse";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private HintCatalog hints;

        private LookHitFinder lookHitFinder;
        private Label hintLabel;
        private string interactKey = string.Empty;
        private string shownHint = string.Empty;

        protected override void ShooterAwake()
        {
            lookHitFinder = GetComponent<LookHitFinder>();
        }

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            string hint = GetHintForLookHit();
            if (hint == shownHint) return;

            shownHint = hint;
            hintLabel.text = hint;
        }

        protected override bool TryBindElements(VisualElement root)
        {
            hintLabel = root.Q<Label>(HintLabelName);
            shownHint = string.Empty;

            if (hintLabel == null)
            {
                Log.Error($"Overlay document has no {HintLabelName} label, interaction hints stay hidden");
                return false;
            }

            if (hints == null)
            {
                Log.Error("Hint overlay has no hint catalog, interaction hints stay hidden");
                return false;
            }

            interactKey = GetInteractKeyName();
            hintLabel.text = string.Empty;
            Log.Info($"Interaction hints are bound to key {interactKey}");

            return true;
        }

        protected override void UnbindElements()
        {
            hintLabel = null;
        }

        private string GetHintForLookHit()
        {
            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            if (playerController == null || !playerController.CanInteract) return string.Empty;

            Interactor interactor = playerController.GetComponent<Interactor>();
            if (!lookHitFinder.TryFindLookHit(out RaycastHit hit) || hit.distance > interactor.ReachDistance) return string.Empty;
            if (hit.collider == null) return string.Empty;

            IUsable usable = hit.collider.GetComponentInParent<IUsable>();
            if (usable == null) return string.Empty;

            return "[" + interactKey + "] " + hints.GetHintText(usable.Usage);
        }

        private static string GetInteractKeyName()
        {
            Controls controls = new();
            InputAction interact = controls.Player.Interact;
            string interactKeyPath = interact.bindings[interact.GetBindingIndex(InputBinding.MaskByGroup(KeyboardScheme))].effectivePath;
            controls.Dispose();

            // Named by the binding path, not by the keyboard: the keyboard names its keys after the system layout, and the Russian one shows E as У
            return InputControlPath.ToHumanReadableString(interactKeyPath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }
}
