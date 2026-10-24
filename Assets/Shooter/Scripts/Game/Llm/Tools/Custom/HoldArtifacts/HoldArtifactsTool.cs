using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class HoldArtifactsTool : LlmTool<HoldArtifactsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryArtifactHolder artifactHolder;

        public override string Name => "hold_artifacts";

        public override string Description => @"
Pass the ID of the slot/slots with the artifact to `holdSlots` to hold it/them.
Pass the ID of the slot/slots with the artifact you have held to `unholdSlots` to unhold it/them.
";


        protected override void Start()
        {
            artifactHolder = Self.GetComponent<InventoryArtifactHolder>();
            if (artifactHolder == null)
            {
                Log.Error($"Entity {Self.name} failed to find inventory artifact holder component required by tool {Name}");
            }
        }

        protected override string Execute(HoldArtifactsArguments arguments, LlmCallContext context)
        {
            if ((arguments.HoldSlots == null || arguments.HoldSlots.Length == 0) &&
                (arguments.UnholdSlots == null || arguments.UnholdSlots.Length == 0))
            {
                return "Nothing passed";
            }

            StringBuilder sb = new StringBuilder();
            if (arguments.HoldSlots != null)
            {
                foreach (int slot in arguments.HoldSlots)
                {
                    InventoryArtifactHolder.TryHoldResultCode result = artifactHolder.TryHoldSlot(slot);
                    switch (result)
                    {
                        case InventoryArtifactHolder.TryHoldResultCode.Ok:
                            sb.AppendLine($"Slot {slot} succesfully held");
                            break;
                        case InventoryArtifactHolder.TryHoldResultCode.SlotAlreadyHeld:
                            sb.AppendLine($"Failed to hold slot {slot}: the slot is already held");
                            break;
                        case InventoryArtifactHolder.TryHoldResultCode.SlotIsEmpty:
                            sb.AppendLine($"Failed to hold slot {slot}: the slot is empty");
                            break;
                        case InventoryArtifactHolder.TryHoldResultCode.SlotNotAnArtifact:
                            sb.AppendLine($"Failed to hold slot {slot}: the slot is not an artifact");
                            break;
                        case InventoryArtifactHolder.TryHoldResultCode.TooManySlots:
                            sb.AppendLine($"Failed to hold slot {slot}: too many slots held");
                            break;
                        default:
                            sb.AppendLine($"Attempt to hold slot {slot} caused unexpected result code {result}");
                            break;
                    }
                }
            }
            if (arguments.UnholdSlots != null)
            {
                foreach (int slot in arguments.UnholdSlots)
                {
                    InventoryArtifactHolder.TryReleaseResultCode result = artifactHolder.TryReleaseSlot(slot);
                    switch (result)
                    {
                        case InventoryArtifactHolder.TryReleaseResultCode.Ok:
                            sb.AppendLine($"Slot {slot} successfully released");
                            break;
                        case InventoryArtifactHolder.TryReleaseResultCode.SlotNotHeld:
                            sb.AppendLine($"Failed to release slot {slot}: the slot was not held");
                            break;
                        default:
                            sb.AppendLine($"Attempt to unhold slot {slot} caused unexpected result code {result}");
                            break;
                    }
                }
            }
            return sb.ToString();
        }
    }
}
