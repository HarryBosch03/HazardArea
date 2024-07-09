using FishNet.Object;
using FishNet.Object.Synchronizing;
using Runtime.Player;
using Runtime.World;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class ItemPickup : NetworkBehaviour
{
    public ItemType type;
    public int startingAmount;
    public int amountPickedUp;
    public bool infinite;

    private readonly SyncVar<int> amount = new SyncVar<int>();

    private Interactable interactable;

    private void Awake() { interactable = GetComponent<Interactable>(); }

    private void OnEnable() { interactable.InteractEvent += OnInteract; }

    private void OnDisable() { interactable.InteractEvent -= OnInteract; }

    private void Start() { interactable.getDisplayText = () => $"Pickup {type.name} ({Mathf.Min(amount.Value, amountPickedUp)}/{(infinite ? "\u221e" : amount.Value)})"; }

    public override void OnStartServer() { amount.Value = startingAmount; }

    [Server]
    private void OnInteract(FPSController player)
    {
        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        if (infinite)
        {
            var stack = new ItemStack(type, amountPickedUp);
            inventory.AddToInventory(ref stack);
        }
        else
        {
            var delta = -amountPickedUp;
            var stack = new ItemStack(type, Mathf.Min(amount.Value, amountPickedUp));
            inventory.AddToInventory(ref stack);
            delta += stack.amount;
            amount.Value += delta;
            if (amount.Value <= 0)
            {
                FinalizePickup();
            }
        }
    }

    private void FinalizePickup() { Despawn(); }
}