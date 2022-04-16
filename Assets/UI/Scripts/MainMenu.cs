using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

/*
 * TODO separate out the connection stuff
 */
public class MainMenu : MonoBehaviour {

    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool connected;

    [SerializeField]
    private TMP_InputField playerName;
    [SerializeField]
    private TMP_InputField serverAddress;
    [SerializeField]
    private TMP_Text lobbyInfo;

    void Start() {
        networkManager = NetworkManager.Singleton;
        transport = networkManager.GetComponentInParent<UnityTransport>();

        serverAddress.text = MultiplayerUtil.GetLocalIPAddress();
        ServerAddressChanged();
    }

    void Update() {
    }

    public void PlayerNameChanged() {
        string name = playerName.name;
        PlayerManager.Instance.LocalPlayer.PlayerName = name;
    }

    public void ServerAddressChanged() {
        string ip = serverAddress.text;
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.ServerListenAddress = ip;
    }

    public void StartHost() {
        connected = connected || NetworkManager.Singleton.StartHost();
    }

    public void StartClient() {
        connected = connected || NetworkManager.Singleton.StartClient();
    }

    public void StartGame() {
        if (!connected) {
            Debug.LogWarning("Cannot start game unless connected!");
            return;
        }
        foreach (PersistentPlayer player in PlayerManager.Instance.Players) {
            player.SpawnAvatar();
        }
    }

    void OnGUI() {
        // TODO only update when needed
        if (connected) {
            lobbyInfo.text = PlayerManager.Instance.LobbyInfo;
        } else {
            lobbyInfo.text = "Not connected";
        }
    }
}
