using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour {

    public static PlayerManager Instance;

    public string LocalPlayerName = "";

    private NetworkVariable<int> _playerCount =
            new NetworkVariable<int>(1);

    private List<PersistentPlayer> _players;
    public List<PersistentPlayer> Players {
        get => _players;
    }

    private PersistentPlayer _localPlayer;
    public PersistentPlayer LocalPlayer {
        get => _localPlayer;
    }

    public string LobbyInfo {
        get {
            string info = _playerCount.Value + "/4: ";
            int n = 0;
            foreach (var player in _players) {
                info = info + ((n++ == 0) ? "" : ", ") + player.PlayerName;
            }
            return info;
        }
    }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
        DontDestroyOnLoad(this);

        _players = new List<PersistentPlayer>();
    }

    void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback +=
            (id) => {
                if (IsServer) {
                    _playerCount.Value++;
                }
            };

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            (id) => {
                if (IsServer) {
                    _playerCount.Value--;
                    PersistentPlayer player =
                            NetworkManager.Singleton.ConnectedClients[id].PlayerObject.GetComponent<PersistentPlayer>();
                    Debug.Log("Disconnect: " + player.PlayerName);
                }
            };
    }

    void FixedUpdate() {
        Shader.SetGlobalInt("_StripeOffset", ((int) (Time.fixedTime * 16)) % 64);
    }

    public void RegisterPlayer(PersistentPlayer player, bool isLocal) {
        _players.Add(player);
        if (isLocal) {
            player.gameObject.transform.position = transform.position;
            player.gameObject.transform.rotation = transform.rotation;
            _localPlayer = player;
            _localPlayer.PlayerName = LocalPlayerName;
        }
    }

    public static void SpawnAvatars(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut) {
        SpawnAvatars();
    }

    public static void SpawnAvatars() {
        foreach (PersistentPlayer p in PlayerManager.Instance.Players) {
            p.SpawnAvatar(PlayerSpawnLocation.GetSpawn());
        }
        PlayerSpawnLocation.SetPlayersToSpawnLocation();
    }

    public static bool IsLocalPlayerAvatar(Collider collider) {
        PlayerAvatar player = collider.GetComponent<PlayerAvatar>();
        return player != null && player.IsOwner;
    }

}
