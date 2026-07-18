using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public abstract class Dream : VisualElement
    {
        public abstract float Weight { get; }

        protected Dream()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.display = DisplayStyle.None;
        }
    }
}
