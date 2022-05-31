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
    private UnityTransport m_Transport;
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
        m_Transport = networkManager.GetComponentInParent<UnityTransport>();

        serverAddress.text = MultiplayerUtil.GetLocalIPAddress();
        ServerAddressChanged();

        playerName.text = SystemInfo.deviceName;
        PlayerNameChanged();

        if (IsArgon()) {
            serverAddress.text = "192.168.0.10";
            ServerAddressChanged();
        }
    }

    public void PlayerNameChanged() {
        string name = playerName.text;
        PlayerManager.Instance.LocalPlayerName = name;
    }

    public void ServerAddressChanged() {
        string ip = serverAddress.text;
        m_Transport.ConnectionData.Address = ip;
        m_Transport.ConnectionData.ServerListenAddress = ip;
    }

    public bool IsArgon() {
        return SystemInfo.deviceName == "argon" && System.Environment.GetEnvironmentVariable("GOOSE") == "y";
    }

    public void StartHost() {
        if (MultiplayerUtil.GetLocalIPAddress() != serverAddress.text) {
            Debug.LogWarning("Cannot host a game from a different ip address");
            return;
        }
        connected = connected || NetworkManager.Singleton.StartHost();
        if (connected) {
            host = true;
        } else {
            m_Transport.Shutdown();
        }
    }

    public void StartClient() {
        if (MultiplayerUtil.GetLocalIPAddress() == serverAddress.text && !IsArgon()) {
            Debug.LogWarning("Cannot join a game from the same ip address, host instead");
            return;
        }

        connected = connected || NetworkManager.Singleton.StartClient();

        if (!connected) {
            m_Transport.Shutdown();
        }
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

        if (host) {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += PlayerManager.Instance.StartShip;
            if (playerName.text != "Demo") {
                NetworkManager.Singleton.SceneManager.LoadScene("ShipScene", LoadSceneMode.Single);
            }
            else {
                NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            }
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
