using Shooter.Infrastructure;
using UnityEngine.UIElements;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Overlay : ShooterBehaviour
    {
        private PanelRenderer panelRenderer;

        protected bool IsBound { get; private set; }

        private void OnEnable()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            panelRenderer.RegisterUIReloadCallback(HandleUIReloaded);
        }

        private void OnDisable()
        {
            panelRenderer.UnregisterUIReloadCallback(HandleUIReloaded);
            UnbindElementsIfBound();
        }

        protected abstract bool TryBindElements(VisualElement root);

        protected virtual void UnbindElements()
        {
        }

        private void HandleUIReloaded(PanelRenderer reloadedPanelRenderer, VisualElement root)
        {
            UnbindElementsIfBound();
            IsBound = TryBindElements(root);
        }

        private void UnbindElementsIfBound()
        {
            if (!IsBound) return;

            IsBound = false;
            UnbindElements();
        }
    }
}
