using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine.SceneManagement;

/*
 * TODO separate out the connection stuff
 */
public class MainMenu : MonoBehaviour {

    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool connected = false;
    private bool host = false;

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
        string name = playerName.text;
        PlayerManager.Instance.LocalPlayerName = name;
    }

    public void ServerAddressChanged() {
        string ip = serverAddress.text;
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.ServerListenAddress = ip;
    }

    public void StartHost() {
        connected = connected || NetworkManager.Singleton.StartHost();
        host = true;
    }

    public void StartClient() {
        connected = connected || NetworkManager.Singleton.StartClient();
    }

    public void StartGame() {
        if (!connected) {
            Debug.LogWarning("Cannot start game unless connected!");
            return;
        }
        if (!host) {
            Debug.LogWarning("Cannot start game as non-host!");
            return;
        }
        foreach (PersistentPlayer player in PlayerManager.Instance.Players) {
            player.SpawnAvatar();
        }

        if (host) {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += PlayerSpawnLocation.SetPlayersToSpawnLocation;
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
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
