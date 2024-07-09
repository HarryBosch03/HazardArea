using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Runtime;
using Runtime.Player;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public int capacity = 8;
#if UNITY_EDITOR
    public ItemStack[] localContent;
#endif

    private readonly SyncList<ItemStack> content = new();

    public event Action OnInventoryChanged;

    public ItemStack this[int i]
    {
        get => content[i];
        set
        {
            content[i] = value;
            OnInventoryChanged?.Invoke();
        }
    }

    private void Awake()
    {
        for (var i = 0; i < capacity; i++) content.Add(default);
        content.OnChange += (_, _, _, _, _) => OnInventoryChanged?.Invoke();

#if UNITY_EDITOR
        localContent = new ItemStack[content.Count];
        OnInventoryChanged += () =>
        {
            content.CopyTo(localContent, 0);
        };
#endif
    }

    [Server]
    public void AddToInventory(ref ItemStack newStack)
    {
        if (newStack.type == null || newStack.amount == 0) return;

        for (var i = 0; i < content.Count; i++)
        {
            var stack = content[i];
            if (stack.type == null) continue;

            if (stack.type == newStack.type)
            {
                if (stack.amount + newStack.amount <= newStack.type.stackSize)
                {
                    stack.amount += newStack.amount;
                    newStack.amount = 0;
                    content[i] = stack;
                    return;
                }
                else
                {
                    var difference = newStack.type.stackSize - stack.amount;
                    stack.amount += difference;
                    newStack.amount -= difference;
                    content[i] = stack;
                }
            }
        }

        for (var i = 0; i < content.Count; i++)
        {
            if (content[i].IsEmpty())
            {
                content[i] = newStack;
                newStack.amount = 0;
                return;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public bool CanFit(ItemType type, int count, out int spaceLeftTotal)
    {
        spaceLeftTotal = 0;

        for (var i = 0; i < content.Count; i++)
        {
            if (content[i].type == type)
            {
                var spaceLeft = type.stackSize - content[i].amount;
                count -= spaceLeft;
                spaceLeftTotal += spaceLeft;
                if (count <= 0) return true;
            }
        }

        return false;
    }

    public int Count(ItemType type)
    {
        var c = 0;
        for (var i = 0; i < content.Count; i++)
        {
            if (content[i].type == type) c += content[i].amount;
        }

        return c;
    }

    [Server]
    public void Consume(ItemType type, int count, out int quantityConsumed)
    {
        quantityConsumed = 0;

        for (var i = 0; i < content.Count; i++)
        {
            var stack = content[i];
            if (stack.type != type) continue;
            {
                var difference = Mathf.Min(stack.amount, count - quantityConsumed);
                stack.amount -= difference;
                quantityConsumed += difference;
                content[i] = stack;
                if (quantityConsumed == count) return;
            }
        }

        OnInventoryChanged?.Invoke();
    }
}