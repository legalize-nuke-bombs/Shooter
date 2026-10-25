using UnityEngine;

namespace Shooter.Infrastructure
{
    public interface IShooterBehaviour
    {
        MonoBehaviour NativeBehaviour { get; }
    }
}
