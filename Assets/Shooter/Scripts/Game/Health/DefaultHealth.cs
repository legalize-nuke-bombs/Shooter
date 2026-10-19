using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public sealed class DefaultHealth : Health, ISaveableComponent
    {
        [SerializeField] private double maxHp = 100;

        private readonly NetworkVariable<double> hp = new();

        public string ComponentKey => "Health";

        private struct SaveData
        {
            public double Hp { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Hp = hp.Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            hp.Value = saveData.Hp;
        }

        public override double Hp => hp.Value;

        public override double MaxHp => Math.Max(maxHp, 1.0d);

        public override bool IsAlive => hp.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            hp.Value = MaxHp;
        }

        protected override void ReduceHp(double amount)
        {
            hp.Value = Math.Max(hp.Value - amount, 0);
        }

        public override void RestoreHp(double amount)
        {
            if (!IsServer || !IsAlive || amount <= 0) return;

            hp.Value = Math.Min(hp.Value + amount, MaxHp);
        }

        public override void ResurrectWithFullHp()
        {
            if (!IsServer || IsAlive) return;

            hp.Value = MaxHp;
        }
    }
}
