using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [RequireComponent(typeof(LookHitFinder))]
    public class HintOverlay : Overlay
    {
        private const string HintLabelName = "hint";
        private const string KeyboardScheme = "Keyboard&Mouse";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private HintCatalog hints;

        private LookHitFinder lookHitFinder;
        private Label hintLabel;
        private string interactKey = string.Empty;
        private string shownHint = string.Empty;

        private void Awake()
        {
            lookHitFinder = GetComponent<LookHitFinder>();
        }

        private void Update()
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
            Interactor interactor = LocalPlayerFinder.FindComponent<Interactor>();
            if (interactor == null) return string.Empty;

            if (!lookHitFinder.TryFindLookHit(out RaycastHit hit) || hit.distance > interactor.ReachDistance) return string.Empty;
            if (hit.collider == null) return string.Empty;

            IUsable usable = hit.collider.GetComponentInParent<IUsable>();
            if (usable == null) return string.Empty;

            return "[" + interactKey + "] " + hints.GetHintText(usable.Usage);
        }

        private static string GetInteractKeyName()
        {
            Controls controls = new();
            string interactKeyName = controls.Player.Interact.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardScheme));
            controls.Dispose();

            return interactKeyName;
        }
    }
}
