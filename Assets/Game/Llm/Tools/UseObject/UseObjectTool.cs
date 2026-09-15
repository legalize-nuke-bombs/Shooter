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
Use (turn on / off, pickup, etc.) the usable object on the map.
This tool attempts to use the nearest usable object on the map within a {radius}-meter radius. Get right up close to the object and then use this tool to try using it.
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
            Vector3 center = Self.transform.position;

            Collider[] colliders = Physics.OverlapSphere(center, radius);

            IUsable nearestUsable = null;
            float minDistanceSqr = Mathf.Infinity;

            foreach (Collider col in colliders)
            {
                if (col.TryGetComponent(out IUsable usable))
                {
                    float distanceSqr = (col.transform.position - center).sqrMagnitude;

                    if (distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                        nearestUsable = usable;
                    }
                }
            }

            if (nearestUsable != null)
            {
                nearestUsable.Use(networkObject, out string result);
                return result;
            }

            return $"No usable objects found within a {radius}-meter radius around you.";
        }
    }
}
