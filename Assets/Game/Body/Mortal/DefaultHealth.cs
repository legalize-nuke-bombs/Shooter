using System;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class DefaultHealth : Health, ISaveableComponent
    {
        [SerializeField] private double maxHp = 100;

        private readonly NetworkVariable<double> hp = new();

        public string ComponentKey => "Health";
        struct SaveData
        {
            public double Hp { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveData()
            {
                Hp = hp.Value
            };
        }
        public void LoadComponent(JToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            hp.Value = sd.Hp;
        }

        public override double Hp => hp.Value;

        public override double MaxHp => Math.Max(maxHp, 1.0d);

        public override bool Alive => hp.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            hp.Value = MaxHp;
        }

        protected override void DamageRaw(double amount)
        {
            hp.Value = Math.Max(hp.Value - amount, 0);
        }

        public override void Heal(double amount)
        {
            if (!IsServer || !Alive || amount <= 0) return;

            hp.Value = Math.Min(hp.Value + amount, MaxHp);
        }

        public override void Resurrect()
        {
            if (!IsServer || Alive) return;

            hp.Value = MaxHp;
        }
    }
}
