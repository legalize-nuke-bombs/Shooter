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
            if (IsServer) hp.Value = MaxHp;

            hp.OnValueChanged += AnnounceIfAliveChanged;
        }

        public override void OnNetworkDespawn()
        {
            hp.OnValueChanged -= AnnounceIfAliveChanged;
        }

        protected override void ReduceHp(double amount)
        {
            hp.Value = Math.Max(hp.Value - amount, 0);
        }

        public override void Heal(double amount)
        {
            if (!IsServer || !IsAlive || amount <= 0) return;

            hp.Value = Math.Min(hp.Value + amount, MaxHp);
        }

        public override void ResurrectWithFullHp()
        {
            if (!IsServer || IsAlive) return;

            hp.Value = MaxHp;
        }

        private void AnnounceIfAliveChanged(double previousHp, double currentHp)
        {
            bool wasAlive = previousHp > 0;
            bool alive = currentHp > 0;
            if (wasAlive != alive) AnnounceAliveChanged(alive);
        }
    }
}
