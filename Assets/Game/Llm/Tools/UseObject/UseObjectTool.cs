using System;
using System.Text;
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
Use (turn on / off, pickup, etc.) usable objects on the map by their coordinates.
You must provide the exact X, Y, and Z coordinates of the objects. The objects must be within a {{radius}}-meter radius from your current position.
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
            if (arguments.Points == null || arguments.Points.Length == 0)
            {
                return "You didn't pass a single point.";
            }
            var sb = new StringBuilder();
            foreach (Point3D point in arguments.Points)
            {
                HandlePoint(point, sb);
            }
            return sb.ToString();
        }

        private void HandlePoint(Point3D point, StringBuilder sb)
        {
            Vector3 selfPosition = Self.transform.position;
            var targetPosition = new Vector3(point.X, point.Y, point.Z);

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

            sb.Append($"[{point.X}, {point.Y}, {point.Z}] ");

            if (targetUsable == null)
            {
                sb.AppendLine("Failed to find usable object here. Make sure the coordinates are precise.");
                return;
            }

            float distance = Vector3.Distance(selfPosition, finalObjectPosition);
            if (distance > radius)
            {
                sb.AppendLine($"The object is too far away ({distance} meters). You must come closer than {radius} meters to use it. Current distance: {distance}m.");
                return;
            }

            targetUsable.Use(networkObject, out string result);
            sb.AppendLine(result);
        }
    }
}
