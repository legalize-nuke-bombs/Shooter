using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Dreaming
{
    public abstract class Dream
    {
        private readonly VisualElement screen;

        protected Dream(VisualElement screen)
        {
            this.screen = screen;
        }

        protected VisualElement Screen => screen;

        public abstract void Step(float dt);

        public virtual void End()
        {
            screen.Clear();
            screen.style.backgroundColor = Color.clear;
            screen.style.backgroundImage = new StyleBackground((Texture2D)null);
        }
    }
}
