using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace Runtime.Player
{
    public class HealthController : NetworkBehaviour
    {
        private static readonly SyncTypeSettings syncSettings = new SyncTypeSettings
        {
            Channel = Channel.Unreliable,
            ReadPermission = ReadPermission.Observers,
            WritePermission = WritePermission.ClientUnsynchronized,
        };

        public float startingMaxHealth = 100f;
        
        public readonly SyncVar<float> currentHealth = new SyncVar<float>(syncSettings);
        public readonly SyncVar<float> maxHealth = new SyncVar<float>(syncSettings);
        public readonly SyncVar<bool> isAlive = new SyncVar<bool>(syncSettings);

        public override void OnStartServer()
        {
            maxHealth.Value = startingMaxHealth;
            Revive(1f);
        }

        [Server]
        private void Revive(float healthPercentage)
        {   
            isAlive.Value = true;
            currentHealth.Value = Mathf.Max(1f, maxHealth.Value * Mathf.Clamp01(healthPercentage));
        }

        [Server]
        public void Damage(DamageArgs args)
        {
            currentHealth.Value -= args.damage;
            if (currentHealth.Value <= 0f)
            {
                Die(args);
            }
            
            NotifyDamage(args);
        }

        [Server]
        private void Die(DamageArgs args)
        {
            isAlive.Value = false;
            NotifyDead();
        }

        [Rpc(RunLocally = true)]
        private void NotifyDead()
        {
            Debug.Log($"{name} is dead");
        }

        [Rpc(RunLocally = true)]
        private void NotifyDamage(DamageArgs args)
        {
            Debug.Log($"{name} has taken {args.damage}");
        }

        [Serializable]
        public struct DamageArgs
        {
            public float damage;
        }
    }
}