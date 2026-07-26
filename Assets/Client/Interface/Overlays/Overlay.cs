using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Overlay : MonoBehaviour
    {
        private PanelRenderer panel;

        protected bool Bound { get; private set; }

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Reload);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Reload);
            Release();
        }

        protected abstract bool Bind(VisualElement root);

        protected virtual void Unbind()
        {
        }

        private void Reload(PanelRenderer renderer, VisualElement root)
        {
            Release();
            Bound = Bind(root);
        }

        private void Release()
        {
            if (!Bound) return;

            Bound = false;
            Unbind();
        }
    }
}
