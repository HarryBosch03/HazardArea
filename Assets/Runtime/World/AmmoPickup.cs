using FishNet.Object;
using FishNet.Object.Synchronizing;
using Runtime.Player;
using Runtime.Weapons;
using Runtime.World;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class AmmoPickup : NetworkBehaviour
{
    public int startingAmount;
    public bool infinite;

    private readonly SyncVar<int> amount = new SyncVar<int>();

    private Interactable interactable;

    private void Awake() { interactable = GetComponent<Interactable>(); }

    private void OnEnable() { interactable.InteractEvent += OnInteract; }

    private void OnDisable() { interactable.InteractEvent -= OnInteract; }

    private void Start()
    {
        interactable.getDisplayText = player =>
        {
            if (player.currentWeapon is not Gun gun) return "Cannot Pickup Ammo";
            return $"Pickup Ammo ({Mathf.Min(amount.Value, gun.magazineSize)}/{(infinite ? "\u221e" : amount.Value)})";
        };
        interactable.canInteractCallback += CanInteract;
    }

    private bool CanInteract(FPSController player) => player.currentWeapon is Gun;

    public override void OnStartServer()
    {
        amount.Value = startingAmount; 
    }

    private void OnInteract(FPSController player)
    {
        if (player.currentWeapon is Gun gun)
        {
            if (infinite)
            {
                gun.data.reserve += gun.magazineSize;
            }
            else
            {
                var consumed = Mathf.Min(amount.Value, gun.magazineSize);
                gun.data.reserve += consumed;
                amount.Value -= consumed;
                
                if (amount.Value <= 0)
                {
                    FinalizePickup();
                }
            }
        }
    }

    private void FinalizePickup() { Despawn(); }
}