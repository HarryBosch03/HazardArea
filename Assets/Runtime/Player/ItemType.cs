using System;
using UnityEngine;

namespace Runtime.Player
{
    [CreateAssetMenu(menuName = "Scriptables/Item Type")]
    public class ItemType : ScriptableObject
    {
        public int stackSize = 1;

        public Sprite icon { get; private set; }

        private void Awake()
        {
            icon = Resources.Load<Sprite>($"Items/Icons/{name}");
        }

        private void OnValidate()
        {
            stackSize = Mathf.Max(1, stackSize);
        }
    }
}
