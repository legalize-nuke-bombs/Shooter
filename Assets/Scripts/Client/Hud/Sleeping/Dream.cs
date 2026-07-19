using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Sleeping
{
    public abstract class Dream : Overlay
    {
        public abstract float Weight { get; }

        protected Dream()
        {
            style.display = DisplayStyle.None;
        }
    }
}
