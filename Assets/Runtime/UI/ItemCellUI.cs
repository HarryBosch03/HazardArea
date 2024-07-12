using System;
using Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

namespace Runtime.UI
{
    public class ItemCellUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public Image icon;
        public TMP_Text countText;
        
        private Sprite missingSprite;
        private ItemStack privateStack;

        public event Action<ItemCellUI, PointerEventData> ClickedEvent;
        
        public ItemStack stack
        {
            get => privateStack;
            set
            {
                privateStack = value;
                if (value.type != null && value.amount > 0)
                {
                    icon.enabled = true;
                    icon.sprite = value.type.icon ? value.type.icon : missingSprite;
                    countText.text = value.amount.ToString();
                }
                else
                {
                    icon.enabled = false;
                    countText.text = "";
                }
            }
        }

        private void Awake()
        {
            missingSprite = icon.sprite;
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            ClickedEvent?.Invoke(this, eventData);
        }

        public void OnPointerDown(PointerEventData eventData) {  }
    }
}