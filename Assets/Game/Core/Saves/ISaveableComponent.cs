namespace Shooter.Game.Core.Saves
{
    public interface ISaveableComponent
    {
        string ComponentKey();
        object SaveComponent();
        void LoadComponent(object content);
    }
}
