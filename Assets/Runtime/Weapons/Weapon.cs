using FishNet.Object;
using Runtime.Player;

namespace Runtime.Weapons
{
    public abstract class Weapon : NetworkBehaviour
    {
        public PlayerController player { get; private set; }
        public string displayName => name;
        public abstract string ammoCountText { get; }
        public abstract bool isReloading { get; }
        public abstract float reloadPercent { get; }
        
        protected virtual void Awake()
        {
            player = GetComponentInParent<PlayerController>();
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        protected abstract void OnTick();
        protected abstract void OnPostTick();
    }
}