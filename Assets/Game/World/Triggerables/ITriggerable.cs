using Shooter.Game.Core;

namespace Shooter.Game.World
{
    public interface ITriggerable
    {
        public void OnTrigger(CharacterId character);
    }
}
