using System.Collections.Generic;
using Runtime.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Runtime.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryUI : MonoBehaviour
    {
        public ItemCellUI cellPrefab;
        public Transform cellParent;
        public float fadeDuration = 0.1f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float alpha;
        private CanvasGroup group;
        private FPSController player;
        private PlayerInventory inventory;
        private List<ItemCellUI> cells = new();

        public bool isOpen
        {
            get => group.interactable;
            set
            {
                group.interactable = value;
                group.blocksRaycasts = value;
                Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
                player.isControlling = !value;
            }
        }
        
        private void Awake()
        {
            player = GetComponentInParent<FPSController>();
            inventory = player.GetComponent<PlayerInventory>();
            group = GetComponent<CanvasGroup>();
            cells = new List<ItemCellUI>(GetComponentsInChildren<ItemCellUI>());
            isOpen = false;
        }

        private void OnEnable()
        {
            Refresh();
            inventory.OnInventoryChanged += Refresh;
            
            foreach (var cell in cells)
            {
                cell.ClickedEvent += OnCellClicked;
            }
        }

        private void Update()
        {
            if (player.IsOwner)
            {
                if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame()) isOpen = !isOpen;
            }
            else
            {
                enabled = false;
            }
            
            alpha = Mathf.MoveTowards(alpha, isOpen ? 1f : 0f, Time.deltaTime / Mathf.Max(fadeDuration, Time.deltaTime));
            group.alpha = fadeCurve.Evaluate(alpha);
        }

        private void Refresh()
        {
            while (cells.Count < inventory.capacity)
            {
                var newCell = Instantiate(cellPrefab, cellParent);
                newCell.name = $"ItemCell{cells.Count}";
                newCell.ClickedEvent += OnCellClicked;
                cells.Add(newCell);
            }
            while (cells.Count > inventory.capacity)
            {
                Destroy(cells[^1].gameObject);
                cells.RemoveAt(cells.Count - 1);
            }

            for (var i = 0; i < inventory.capacity; i++)
            {
                cells[i].stack = inventory[i];
            }
        }

        private void OnDisable()
        {
            inventory.OnInventoryChanged -= Refresh;

            foreach (var cell in cells)
            {
                cell.ClickedEvent -= OnCellClicked;
            }
        }

        private void OnCellClicked(ItemCellUI cell, PointerEventData args)
        {
            if (args.button == PointerEventData.InputButton.Right)
            {
                inventory.SplitStack(cells.IndexOf(cell));
            }
        }
    }
}