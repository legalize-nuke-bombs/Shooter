using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Behavior;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public class SaveableBehaviorGraphAgent : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private BehaviorGraphAgent agent;
        private Dictionary<string, SaveToken> loadedVariables;

        private void Awake()
        {
            agent = GetComponent<BehaviorGraphAgent>();
        }

        public object SaveObject()
        {
            BlackboardReference blackboard = agent.BlackboardReference;
            if (blackboard == null) return null;

            var savedVariables = new Dictionary<string, SaveToken>();
            foreach (BlackboardVariable variable in blackboard.Blackboard.Variables)
            {
                if (TrySave(variable, out SaveToken saved)) savedVariables[variable.Name] = saved;
            }
            return savedVariables;
        }

        public void LoadObject(SaveToken content)
        {
            loadedVariables = content.ToObject<Dictionary<string, SaveToken>>();
        }

        public void PostLoad()
        {
            BlackboardReference blackboard = agent.BlackboardReference;
            if (loadedVariables == null || blackboard == null) return;

            foreach (BlackboardVariable variable in blackboard.Blackboard.Variables)
            {
                if (loadedVariables.Remove(variable.Name, out SaveToken saved))
                {
                    variable.ObjectValue = Load(variable, saved);
                }
            }

            foreach (string variableName in loadedVariables.Keys)
            {
                Log.Warn($"Entity {name} found unknown saved variable {variableName}");
            }
            loadedVariables = null;
        }

        private bool TrySave(BlackboardVariable variable, out SaveToken saved)
        {
            saved = default;
            if (!IsReference(variable.Type))
            {
                saved = SaveToken.FromObject(variable.ObjectValue);
                return true;
            }

            if (!TryGetReferenceId(variable.ObjectValue as Object, out long id))
            {
                Log.Warn($"Entity {name} cannot save variable {variable.Name}: {variable.ObjectValue} carries no IncrementalId");
                return false;
            }

            saved = SaveToken.FromObject(id);
            return true;
        }

        private object Load(BlackboardVariable variable, SaveToken saved)
        {
            if (!IsReference(variable.Type)) return saved.ToObject(variable.Type);

            long id = saved.ToObject<long>();
            if (id == IncrementalId.None) return null;

            IncrementalId target = IncrementalId.FindByValue(id, RegisterInactivePolicy.Include);
            if (target == null)
            {
                Log.Warn($"Entity {name} found no entity with id {id} for variable {variable.Name}");
                return null;
            }

            return variable.Type == typeof(GameObject) ? target.gameObject : target.GetComponent(variable.Type);
        }

        private static bool IsReference(Type type)
        {
            return typeof(Object).IsAssignableFrom(type);
        }

        private static bool TryGetReferenceId(Object reference, out long id)
        {
            id = IncrementalId.None;
            if (reference == null) return true;

            GameObject referencedObject = reference as GameObject;
            if (referencedObject == null)
            {
                Component component = reference as Component;
                if (component == null) return false;
                referencedObject = component.gameObject;
            }

            IncrementalId incrementalId = referencedObject.GetComponent<IncrementalId>();
            if (incrementalId == null) return false;

            id = incrementalId.Value;
            return true;
        }
    }
}
