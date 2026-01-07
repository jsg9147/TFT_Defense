using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class NetworkTestUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button shutdownButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(() => NetworkGameManager.Instance?.StartHost());
        
        if (clientButton != null)
            clientButton.onClick.AddListener(() => NetworkGameManager.Instance?.StartClient());
        
        if (shutdownButton != null)
            shutdownButton.onClick.AddListener(() => NetworkGameManager.Instance?.Shutdown());
    }

    private void Update()
    {
        if (statusText != null && NetworkGameManager.Instance != null)
        {
            var nm = NetworkGameManager.Instance;
            statusText.text = $"Server: {nm.IsServer}\n" +
                            $"Client: {nm.IsClient}\n" +
                            $"Host: {nm.IsHost}\n" +
                            $"Connected: {nm.IsConnected}\n" +
                            $"Players: {nm.GetConnectedPlayerCount()}";
        }
    }
}