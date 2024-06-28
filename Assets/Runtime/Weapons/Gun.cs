using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Runtime.Player;
using UnityEngine;

namespace Runtime.Weapons
{
    public class Gun : Weapon<Gun.ReconciliationData>
    {
        public Projectile projectilePrefab;
        
        public Projectile.ProjectileSpawnArgs projectileSpawnArgs;
        public float firerate;
        public bool singleFire;
        public float aimTime;
        public int magazineSize;
        public float reloadTime;

        public ParticleSystem flash;
        
        public ReconciliationData data;

        public event Action OnShoot;
        public event Action OnReload;

        public float aimPercent => data.aimPercent;
        
        protected override void OnTick()
        {
            Replicate(default);
        }

        private void Shoot()
        {
            if (data.shootTimer < 0 && data.magazine > 0)
            {
                data.shootTimer = 60f / firerate;
                projectilePrefab.Spawn(player.head, player.body.linearVelocity, player.NetworkObject, projectileSpawnArgs);
                OnShoot?.Invoke();
                if (flash) flash.Play();
                data.magazine--;
            }
        }


        protected override void OnPostTick()
        {
            CreateReconcile();    
        }
        
        [Replicate]
        private void Replicate(EmptyReplicationData _, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (data.reloading)
            {
                data.reloadTimer -= (float)TimeManager.TickDelta;
                if (data.reloadTimer <= 0f)
                {
                    data.reloading = false;
                    data.magazine = magazineSize;
                }
            }
            else
            {
                if (player.input.shoot.pressedThisTick || player.input.shoot && !singleFire)
                {
                    Shoot();
                }

                if (player.input.reload.pressedThisTick && data.magazine < magazineSize)
                {
                    data.reloading = true;
                    data.reloadTimer = reloadTime;
                    data.magazine = 0;
                    OnReload?.Invoke();
                }
            }

            var aiming = player.input.aim && player.moveState != PlayerController.MoveState.Sprint && !data.reloading;
            data.aimPercent = Mathf.MoveTowards(data.aimPercent, aiming ? 1f : 0f, (float)TimeManager.TickDelta / aimTime);
            data.shootTimer -= Time.fixedDeltaTime;
        }
        
        public override void CreateReconcile()
        {
            Reconcile(data);
        }

        [Reconcile]
        private void Reconcile(ReconciliationData data, Channel channel = Channel.Unreliable)
        {
            this.data = data;
        }

        public struct ReconciliationData : IReconcileData
        {
            public float shootTimer;
            public float aimPercent;
            public int magazine;
            public float reloadTimer;
            public bool reloading;
            
            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
        }
    }
}
