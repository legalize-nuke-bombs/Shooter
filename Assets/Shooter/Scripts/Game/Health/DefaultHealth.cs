using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
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
            SetHp(saveData.Hp);
        }

        public override double Hp => hp.Value;

        public override double MaxHp => Math.Max(maxHp, 1.0d);

        public override bool IsAlive => hp.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                hp.Value = MaxHp;
            }
            else
            {
                hp.OnValueChanged += HandleHpReplicated;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) return;

            hp.OnValueChanged -= HandleHpReplicated;
        }

        protected override void ReduceHp(double amount)
        {
            SetHp(Math.Max(hp.Value - amount, 0));
        }

        public override void Heal(double amount)
        {
            if (!IsServer || !IsAlive || amount <= 0) return;

            SetHp(Math.Min(hp.Value + amount, MaxHp));
        }

        public override void ResurrectWithFullHp()
        {
            if (!IsServer || IsAlive) return;

            SetHp(MaxHp);
        }

        private void SetHp(double value)
        {
            bool wasAlive = IsAlive;
            hp.Value = value;
            if (IsAlive != wasAlive) AnnounceAliveChanged(IsAlive);
        }

        private void HandleHpReplicated(double previousHp, double currentHp)
        {
            bool wasAlive = previousHp > 0;
            bool alive = currentHp > 0;
            if (wasAlive != alive) AnnounceAliveChanged(alive);
        }
    }
}
