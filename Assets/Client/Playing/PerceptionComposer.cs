using Shooter.Game.Body.Perception;
using UnityEngine;

namespace Shooter.Client.Playing
{
    [DefaultExecutionOrder(-10)]
    public class PerceptionComposer : MonoBehaviour, IPerceiver
    {
        public Vector3 CameraSway { get; set; }

        private void Update()
        {
            CameraSway = Vector3.zero;
        }
    }
}
