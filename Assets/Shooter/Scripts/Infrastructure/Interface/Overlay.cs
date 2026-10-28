using UnityEngine.UIElements;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Overlay : MonoBehaviour
    {
        private PanelRenderer panelRenderer;

        protected bool IsBound { get; private set; }

        protected virtual void OnEnable()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            panelRenderer.RegisterUIReloadCallback(HandleUIReloaded);
        }

        protected virtual void OnDisable()
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
