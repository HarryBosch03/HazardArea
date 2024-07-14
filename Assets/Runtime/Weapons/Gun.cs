using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Runtime.Player;
using Runtime.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Weapons
{
    public class Gun : Weapon
    {
        public Projectile projectilePrefab;

        public Projectile.ProjectileSpawnArgs projectileSpawnArgs;
        public float firerate;
        public bool singleFire;
        public float aimTime;
        public AnimationCurve aimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public int magazineSize;
        public float reloadTime;
        public float equipTime;
        public float aimFieldOfView = 60f;

        [Space]
        public Vector2 recoilForce;
        public float recoilDecay;

        public ParticleSystem flash;

        public ReconciliationData data;

        public event Action OnFire;
        public event Action OnReload;

        private float aimPercent;

        public float smoothedAimPercent => aimCurve.Evaluate(aimPercent);
        public override string ammoCountText => $"{data.magazine}/{data.reserve}";
        public override bool isReloading => data.reloading;
        public override float reloadPercent => 1f - data.reloadTimer / reloadTime;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            data.magazine = magazineSize;
        }

        protected override void OnTick() { Replicate(default); }

        private void Shoot()
        {
            if (data.shootTimer < 0)
            {
                if (data.magazine > 0)
                {
                    data.shootTimer = 60f / firerate;
                    projectilePrefab.Spawn(player.head, player.body.linearVelocity, player.NetworkObject, projectileSpawnArgs);
                    OnFire?.Invoke();
                    if (flash) flash.Play();
                    data.magazine--;

                    data.recoilVelocity += new Vector2
                    {
                        x = Mathf.Lerp(-recoilForce.x, recoilForce.x, Random.value),
                        y = recoilForce.y,
                    };
                }
                else
                {
                    Reload();
                }
            }
        }


        protected override void OnPostTick() { CreateReconcile(); }

        [Replicate]
        private void Replicate(EmptyReplicationData _, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (isActiveAndEnabled)
            {
                if (((int)TimeManager.Tick - data.lastDisabledTick) / (float)TimeManager.TickRate < equipTime) { }
                else if (data.reloading)
                {
                    data.reloadTimer -= (float)TimeManager.TickDelta;
                    if (data.reloadTimer <= 0f)
                    {
                        data.reloading = false;
                        var consumed = Mathf.Min(data.reserve, magazineSize);
                        data.magazine += consumed;
                        data.reserve -= consumed;
                    }
                }
                else
                {
                    if (player.tickInput.shoot.pressedThisTick || player.tickInput.shoot && !singleFire)
                    {
                        Shoot();
                    }

                    if (player.tickInput.reload.pressedThisTick)
                    {
                        Reload();
                    }
                }

                data.recoilVelocity -= data.recoilVelocity * recoilDecay * TimeUtil.tickDelta;
                player.rotation += data.recoilVelocity * TimeUtil.tickDelta;

                var aiming = player.tickInput.aim && player.moveState != FPSController.MoveState.Sprint && player.onGround && !data.reloading;
                aimPercent = Mathf.MoveTowards(aimPercent, aiming ? 1f : 0f, (float)TimeManager.TickDelta / aimTime);
                data.shootTimer -= Time.fixedDeltaTime;
            }
            else
            {
                aimPercent = 0f;
                data.lastDisabledTick = (int)TimeManager.Tick;
                data.reloading = false;
            }
        }

        private void Reload()
        {
            if (data.magazine == magazineSize) return;
            if (data.reserve == 0) return;
            
            data.reloading = true;
            data.reloadTimer = reloadTime;
            data.reserve += data.magazine;
            data.magazine = 0;
            OnReload?.Invoke();
        }

        public override void CreateReconcile() { Reconcile(data); }

        [Reconcile]
        private void Reconcile(ReconciliationData data, Channel channel = Channel.Unreliable) { this.data = data; }

        public struct ReconciliationData : IReconcileData
        {
            public float shootTimer;
            public int lastDisabledTick;
            public int magazine;
            public int reserve;
            public float reloadTimer;
            public bool reloading;
            public Vector2 recoilVelocity;

            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
        }
    }
}