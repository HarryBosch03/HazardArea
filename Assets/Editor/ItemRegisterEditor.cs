using Runtime.Networking;
using Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(ItemRegister))]
    public class ItemRegisterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var register = target as ItemRegister;
            if (GUILayout.Button("Refresh"))
            {
                register.registeredItems.Clear();
                var guids = AssetDatabase.FindAssets($"t:{nameof(ItemType)}");
                foreach (var guid in guids)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ItemType>(AssetDatabase.GUIDToAssetPath(guid));
                    register.registeredItems.Add(asset);
                }
            }
        }
    }
}