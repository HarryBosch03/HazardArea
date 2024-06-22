using FishNet.Managing;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkManager))]
public class DebugNetworkUI : MonoBehaviour
{
    private NetworkManager netManager;

    private void Awake() { netManager = GetComponent<NetworkManager>(); }

    private void Update()
    {
        if (!netManager.IsClientStarted && !netManager.IsServerStarted)
        {
            var kb = Keyboard.current;
            if (kb.hKey.wasPressedThisFrame) StartHost();
            if (kb.cKey.wasPressedThisFrame) StartClient();
        }
    }

    private void StartHost()
    {
        netManager.ServerManager.StartConnection();
        StartClient();
    }

    private void StartClient()
    {
        netManager.ClientManager.StartConnection();
    }
    
    private void OnGUI()
    {
        using (new GUILayout.AreaScope(new Rect(10f, 10f, 300f, 300f)))
        {
            if (!netManager.IsClientStarted && !netManager.IsServerStarted)
            {
                if (GUILayout.Button("Start Host"))
                {
                    StartHost();
                }
                if (GUILayout.Button("Start Client"))
                {
                    StartClient();
                }
            }
        }
    }
}