using UnityEngine;

namespace Runtime.Player
{
    [CreateAssetMenu(menuName = "Scriptables/Item Type")]
    public class ItemType : ScriptableObject
    {
        public int stackSize = 1;

        public Sprite icon { get; private set; }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            var path = $"Items/Icons/{name}";
            icon = Resources.Load<Sprite>(path);
            if (icon == null) Debug.LogError($"Item {name} could not find icon at \"{path}\"", this);
        }

        private void OnValidate()
        {
            stackSize = Mathf.Max(1, stackSize);
        }
    }
}
