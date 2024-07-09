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
        public ItemType ammoUsed;
        public float equipTime;
        public float aimFieldOfView = 60f;

        [Space]
        public Vector2 recoilForce;
        public float recoilDecay;

        public ParticleSystem flash;
        public PlayerInventory inventory;

        public ReconciliationData data;

        public event Action OnFire;
        public event Action OnReload;

        private float aimPercent;
        private int ammoInInventory;

        public float smoothedAimPercent => aimCurve.Evaluate(aimPercent);
        public override string ammoCountText => $"{data.magazine}/{(ammoInInventory >= 0 ? ammoInInventory : "\u221e")}";
        public override bool isReloading => data.reloading;
        public override float reloadPercent => 1f - data.reloadTimer / reloadTime;

        protected override void Awake()
        {
            base.Awake();
            inventory = player.GetComponent<PlayerInventory>();
        }

        public override void OnStartServer() { data.magazine = magazineSize; }

        private void OnEnable()
        {
            player.activeWeapon = this;
            if (inventory) inventory.OnInventoryChanged += RecountAmmo;
            RecountAmmo();
        }

        private void OnDisable() { if (inventory) inventory.OnInventoryChanged -= RecountAmmo; }

        private void RecountAmmo() { ammoInInventory = inventory && ammoUsed ? inventory.Count(ammoUsed) : -1; }

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
                        if (inventory != null && ammoUsed != null)
                        {
                            inventory.Consume(ammoUsed, magazineSize, out var consumed);
                            data.magazine = consumed;
                        }
                        else
                        {
                            data.magazine = magazineSize;
                        }
                    }
                }
                else
                {
                    if (player.input.shoot.pressedThisTick || player.input.shoot && !singleFire)
                    {
                        Shoot();
                    }

                    if (player.input.reload.pressedThisTick)
                    {
                        Reload();
                    }
                }

                data.recoilVelocity -= data.recoilVelocity * recoilDecay * TimeUtil.tickDelta;
                player.rotation += data.recoilVelocity * TimeUtil.tickDelta;

                var aiming = player.input.aim && player.moveState != FPSController.MoveState.Sprint && player.onGround && !data.reloading;
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
            RecountAmmo();
            if (data.magazine == magazineSize) return;
            if (ammoInInventory == 0) return;
            
            data.reloading = true;
            data.reloadTimer = reloadTime;
            if (IsServerInitialized)
            {
                var stack = new ItemStack(ammoUsed, data.magazine);
                inventory.AddToInventory(ref stack);
            }
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