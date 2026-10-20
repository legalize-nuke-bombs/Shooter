using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [RequireComponent(typeof(LookHitFinder))]
    public class TargetNameOverlay : Overlay
    {
        private const string NameLabelName = "target-name";
        private static readonly Journal Log = Logs.Here();

        private LookHitFinder lookHitFinder;
        private string shownName = string.Empty;
        private Label nameLabel;

        private void Awake()
        {
            lookHitFinder = GetComponent<LookHitFinder>();
        }

        private void Update()
        {
            if (!IsBound) return;

            Nameable nameable = FindNameableAtLookHit();
            string targetName = nameable == null ? string.Empty : nameable.GetShownName();

            if (targetName == shownName) return;

            shownName = targetName;
            nameLabel.text = targetName;
        }

        protected override bool TryBindElements(VisualElement root)
        {
            nameLabel = root.Q<Label>(NameLabelName);
            shownName = string.Empty;

            if (nameLabel == null)
            {
                Log.Error($"Overlay document has no {NameLabelName} label, target names stay hidden");
                return false;
            }

            nameLabel.text = string.Empty;

            return true;
        }

        protected override void UnbindElements()
        {
            nameLabel = null;
        }

        private Nameable FindNameableAtLookHit()
        {
            if (!lookHitFinder.TryFindLookHit(out RaycastHit hit)) return null;
            if (hit.collider == null) return null;

            return hit.collider.GetComponentInParent<Nameable>();
        }
    }
}
