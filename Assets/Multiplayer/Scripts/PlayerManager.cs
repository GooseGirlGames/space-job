using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour {

    public static PlayerManager Instance;

    public string LocalPlayerName = "";

    private NetworkVariable<int> playerCount =
            new NetworkVariable<int>(1);

    private List<PersistentPlayer> players;
    public List<PersistentPlayer> Players {
        get {
            return players;
        }
    }

    private PersistentPlayer localPlayer;
    public PersistentPlayer LocalPlayer {
        get {
            return localPlayer;
        }
    }

    public string LobbyInfo {
        get {
            string info = playerCount.Value + "/4: ";
            foreach (var player in players) {
                info = info + ", " + player.PlayerName;
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

        players = new List<PersistentPlayer>();
    }

    void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback +=
            (id) => {
                if (IsServer) {
                    Debug.Log("New player " + id);
                    playerCount.Value++;
                }
            };

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            (id) => {
                if (IsServer) {
                    playerCount.Value--;
                }
            };
    }

    void Update() {

    }

    public void RegisterPlayer(PersistentPlayer player, bool isLocal) {
        players.Add(player);
        if (isLocal) {
            player.gameObject.transform.position = transform.position;
            player.gameObject.transform.rotation = transform.rotation;
            localPlayer = player;
            localPlayer.PlayerName = LocalPlayerName;
        }
    }

}
