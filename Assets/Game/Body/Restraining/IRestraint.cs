namespace Shooter.Game.Body
{
    public interface IRestraint
    {
        bool CanPerform(ActionType type, float dt);
        void RegisterAction(ActionType type, float dt);
    }
}
