using System;

namespace Shooter.Client.Playing
{
    // Something on the player's body that now and then has a toast to show; whoever draws toasts finds these and listens
    public interface IToastSource
    {
        event Action<Toast> Toasted;
    }
}
