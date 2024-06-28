using FishNet.Object;
using FishNet.Object.Prediction;
using Runtime.Player;

namespace Runtime.Weapons
{
    public abstract class Weapon<T> : NetworkBehaviour where T : struct, IReconcileData
    {
        public PlayerController player { get; private set; }
        
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