using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour {

    public static PlayerManager Instance;

    private NetworkVariable<int> playerCount =
            new NetworkVariable<int>();

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
}
