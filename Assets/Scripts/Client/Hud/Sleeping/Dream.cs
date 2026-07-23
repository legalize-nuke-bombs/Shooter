using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Sleeping
{
    public abstract class Dream : UiElement
    {
        public abstract float Weight { get; }

        protected Dream()
        {
            Fullscreen();
            Visible = false;
        }
    }
}
