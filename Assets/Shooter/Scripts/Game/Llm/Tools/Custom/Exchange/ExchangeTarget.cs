using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class ExchangeTarget
    {
        public static bool TryFind(long targetId, bool taking, InventoryExchanger exchanger, out Inventory target, out string refusal)
        {
            target = null;
            refusal = null;

            IncrementalId found = IncrementalId.FindByValue(targetId, RegisterInactivePolicy.Exclude);
            if (found == null)
            {
                refusal = $"Entity with ID {targetId} does not exist";
                return false;
            }

            if (found.gameObject == exchanger.gameObject)
            {
                refusal = $"ID {targetId} is you";
                return false;
            }

            target = found.GetComponent<Inventory>();
            if (target == null)
            {
                refusal = $"Entity ID {targetId} can not hold items";
                return false;
            }

            if (taking && !target.TryGetComponent(out Lootable _))
            {
                refusal = $"Entity ID {targetId} is not lootable: things are taken only from chests, corpses and the like, a character has to give them to you";
                return false;
            }

            float distance = Vector3.Distance(exchanger.transform.position, target.transform.position);
            if (distance > exchanger.Radius)
            {
                refusal = $"Entity ID {targetId} is {distance:F1} m away, come within {exchanger.Radius} m of it";
                return false;
            }

            return true;
        }
    }
}
