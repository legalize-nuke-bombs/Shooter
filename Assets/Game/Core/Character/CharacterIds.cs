using System;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(-110)]
    public class CharacterIds : MonoBehaviour, ISaveableComponent
    {
        private long next;

        public static CharacterIds Current { get; private set; }

        public string ComponentKey => "CharacterIds";
        private struct SaveData
        {
            public long Next { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveData
            {
                Next = next
            };
        }
        public void LoadComponent(JToken content)
        {
            next = Math.Max(next, content.ToObject<SaveData>().Next);
        }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public long Next()
        {
            return next++;
        }
    }
}
