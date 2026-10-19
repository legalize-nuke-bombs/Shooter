using System;

namespace Shooter.Game
{
    public interface IToastSource
    {
        event Action<Toast> ToastRaised;
    }
}
