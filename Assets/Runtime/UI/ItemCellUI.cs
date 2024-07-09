using Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class ItemCellUI : MonoBehaviour
    {
        public Image icon;
        public TMP_Text countText;
        
        private Sprite missingSprite;
        private ItemStack privateStack;
        
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
    }
}