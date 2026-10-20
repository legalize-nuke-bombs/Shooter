using System;
using System.Text;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [Serializable]
    public sealed class UseObjectTool : LlmTool<UseObjectArguments>
    {
        private const float PointSearchRadius = 1.5f;

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float radius = 5;

        private NetworkObject networkObject;

        public override string Name => "use_object";

        public override string Description =>
            @$"
Use (turn on / off, pickup, etc.) usable objects on the map by their coordinates.
You must provide the exact X, Y, and Z coordinates of the objects. The objects must be within a {radius}-meter radius from your current position.
";

        protected override void HandleAttached()
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
            StringBuilder result = new();
            foreach (Point3D point in arguments.Points)
            {
                UseObjectNearPoint(point, result);
            }
            return result.ToString();
        }

        private void UseObjectNearPoint(Point3D point, StringBuilder result)
        {
            Vector3 selfPosition = Self.transform.position;
            Vector3 pointPosition = new(point.X, point.Y, point.Z);

            Collider[] collidersNearPoint = Physics.OverlapSphere(pointPosition, PointSearchRadius);

            IUsable nearestUsable = null;
            float nearestSqrDistance = Mathf.Infinity;
            Vector3 nearestUsablePosition = Vector3.zero;

            foreach (Collider colliderNearPoint in collidersNearPoint)
            {
                IUsable usable = colliderNearPoint.GetComponentInParent<IUsable>();
                if (usable == null) continue;

                Vector3 usablePosition = ((Component)usable).transform.position;
                float sqrDistance = (usablePosition - pointPosition).sqrMagnitude;

                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestUsable = usable;
                    nearestUsablePosition = usablePosition;
                }
            }

            result.Append($"[{point.X}, {point.Y}, {point.Z}] ");

            if (nearestUsable == null)
            {
                result.AppendLine("Failed to find usable object here. Make sure the coordinates are precise.");
                return;
            }

            float distance = Vector3.Distance(selfPosition, nearestUsablePosition);
            if (distance > radius)
            {
                result.AppendLine($"The object is {distance:F1} meters away. You must come closer than {radius} meters to use it.");
                return;
            }

            nearestUsable.Use(networkObject, out string promptResult);
            result.AppendLine(promptResult);
        }
    }
}
