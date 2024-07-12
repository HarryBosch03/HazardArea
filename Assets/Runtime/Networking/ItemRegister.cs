using System.Collections.Generic;
using Runtime.Player;
using UnityEngine;

namespace Runtime.Networking
{
    public class ItemRegister : MonoBehaviour
    {
        private static ItemRegister privateInstance;
        public static ItemRegister instance
        {
            get
            {
                if (privateInstance == null) privateInstance = FindFirstObjectByType<ItemRegister>();
                return privateInstance;
            }
        }
        
        public List<ItemType> registeredItems = new();

        public int this[ItemType type] => registeredItems.IndexOf(type);
        public ItemType this[int i] => registeredItems[i];
    }
}