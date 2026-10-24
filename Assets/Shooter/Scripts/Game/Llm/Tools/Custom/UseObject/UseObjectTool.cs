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
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float radius = 5;

        private NetworkObject networkObject;

        public override string Name => "use_object";

        public override string Description =>
            @$"
Use (turn on / off, pickup, etc.) usable objects on the map by their IDs.
You must provide the IDs of the objects. The objects must be within a {radius}-meter radius from your current position.
";

        protected override void Start()
        {
            networkObject = Self.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Entity {Self.name} failed to find network object component required by tool {Name}");
            }
        }

        protected override string Execute(UseObjectArguments arguments, LlmCallContext context)
        {
            if (arguments.Ids == null || arguments.Ids.Length == 0)
            {
                return "You didn't pass a single object.";
            }
            StringBuilder result = new();
            foreach (long id in arguments.Ids)
            {
                UseObjectById(id, result);
            }
            return result.ToString();
        }

        private void UseObjectById(long id, StringBuilder result)
        {
            IncrementalId target = IncrementalId.FindByValue(id, RegisterInactivePolicy.Exclude);
            if (target == null)
            {
                result.AppendLine($"Object with ID {id} does not exist");
                return;
            }

            float dst = Vector3.Distance(target.transform.position, Self.transform.position);
            if (dst > radius)
            {
                result.AppendLine($"The object ID {target.Value} is in {dst} m away. Approach the object to a distance of {radius} meters or closer to use it.");
                return;
            }

            IUsable usable = target.GetComponent<IUsable>();
            if (usable == null)
            {
                result.AppendLine($"The object ID {target.Value} is not usable");
                return;
            }

            usable.Use(networkObject, out string promptResult);
            result.AppendLine($"The object ID {target.Value} used: {promptResult}");
        }
    }
}
