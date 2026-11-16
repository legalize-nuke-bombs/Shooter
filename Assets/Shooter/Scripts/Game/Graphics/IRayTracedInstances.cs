using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    public interface IRayTracedInstances
    {
        void AddInstancesTo(AccelerationStructureBuilder builder);
    }
}
