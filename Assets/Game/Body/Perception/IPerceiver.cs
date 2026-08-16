using UnityEngine;

namespace Shooter.Game.Body.Perception
{
    public interface IPerceiver
    {
        Vector3 CameraSway { get; set; }
    }
}
