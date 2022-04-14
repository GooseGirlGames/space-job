using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour {

    public static PlayerManager Instance;

    private NetworkVariable<int> playerCount =
            new NetworkVariable<int>();

    private List<PlayerControllerMP> players;
    private PlayerControllerMP localPlayer;

    public int PlayersInGame {
        get {
            return playerCount.Value;
        }
    }

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
        DontDestroyOnLoad(this);

        players = new List<PlayerControllerMP>();
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

    public void RegisterPlayer(PlayerControllerMP player, bool isLocal) {
        players.Add(player);
        if (isLocal) {
            localPlayer = player;
        }
    }

    public PlayerControllerMP LocalPlayer {
        get {
            return localPlayer;
        }
    }
}
