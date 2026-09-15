using System;
using Shooter.Game.Body;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm.UseObject
{
    [Serializable]
    public sealed class UseObjectTool : LlmTool<UseObjectArguments>
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float radius = 5;

        private NetworkObject networkObject;

        public override string Name => "use_object";

        public override string Description =>
            @$"
Use (turn on / off, pickup, etc.) a specific usable object on the map by its coordinates.
You must provide the exact X, Y, and Z coordinates of the object. The object must be within a {{radius}}-meter radius from your current position.
";

        protected override void OnStart()
        {
            networkObject = Self.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Entity {Self.name} failed to find network object component required by tool {Name}");
            }
        }


        protected override string Execute(UseObjectArguments arguments, LlmCallContext context)
        {
            Vector3 selfPosition = Self.transform.position;
            var targetPosition = new Vector3(arguments.X, arguments.Y, arguments.Z);

            Collider[] colliders = Physics.OverlapSphere(targetPosition, 1.0f);

            IUsable targetUsable = null;
            float minDistanceToTargetSqr = Mathf.Infinity;
            Vector3 finalObjectPosition = Vector3.zero;

            foreach (Collider col in colliders)
            {
                if (col.TryGetComponent(out IUsable usable))
                {
                    float distanceSqr = (col.transform.position - targetPosition).sqrMagnitude;

                    if (distanceSqr < minDistanceToTargetSqr)
                    {
                        minDistanceToTargetSqr = distanceSqr;
                        targetUsable = usable;
                        finalObjectPosition = col.transform.position;
                    }
                }
            }

            if (targetUsable == null)
            {
                return $"Failed to find any usable object at the coordinates ({arguments.X}, {arguments.Y}, {arguments.Z}). Make sure the coordinates are precise.";
            }

            float distanceToPlayer = Vector3.Distance(selfPosition, finalObjectPosition);
            if (distanceToPlayer > radius)
            {
                return $"The object is too far away ({distanceToPlayer} meters). You must come closer than {radius} meters to use it. Current distance: {distanceToPlayer}m.";
            }

            targetUsable.Use(networkObject, out string result);
            return result;
        }
    }
}
