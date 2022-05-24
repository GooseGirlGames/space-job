using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour {

    public static PlayerManager Instance;

    public string LocalPlayerName = "";
    public string LocalPlayerStatus = "";
    private int m_PlayersReady = 0;
    public int PlayersReady{
        get => m_PlayersReady;
        set {
            m_PlayersReady = value;
        }
    }

    private NetworkVariable<int> m_PlayerCount =
            new NetworkVariable<int>(1);

    private List<PersistentPlayer> m_Players;
    public List<PersistentPlayer> Players {
        get => m_Players;
    }

    private PersistentPlayer m_LocalPlayer;
    public PersistentPlayer LocalPlayer {
        get => m_LocalPlayer;
    }

    public string LobbyInfo {
        get {
            string info = m_PlayerCount.Value + "/4: ";
            int n = 0;
            foreach (var player in m_Players) {
                info =((n++ == 0) ? "" : info) +  player.PlayerName + " just joined! \n";
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

        m_Players = new List<PersistentPlayer>();
    }

    // Set to true after the ship scene is loaded
    private bool m_InGame = false;
    public bool InGame {
        get => m_InGame;
    }

    void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback +=
            (id) => {
                if (IsServer) {
                    m_PlayerCount.Value++;

                    NetworkObject po = NetworkManager.Singleton.ConnectedClients[id].PlayerObject;
                    PersistentPlayer player = po.GetComponent<PersistentPlayer>();

                    if (m_InGame) {  // player joined in-progress game
                        Debug.Log("Persistenplayer spawned? " + player.IsSpawned);
                        player.SpawnAvatar(PlayerSpawnLocation.GetSpawn());  // TODO spawn as dead in medbay
                        Debug.Log("Persistenplayer spawned? " + player.IsSpawned);
                    }

                    Debug.Log("Connect: " + player.PlayerName);
                }
            };

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            (id) => {
                if (IsServer) {
                    m_PlayerCount.Value--;
                    NetworkObject po = NetworkManager.Singleton.ConnectedClients[id].PlayerObject;
                    PersistentPlayer player = po.GetComponent<PersistentPlayer>();
                    PlayerAvatar avatar = player.Avatar;

                    // disown
                    po.ChangeOwnership(OwnerClientId);
                    avatar.GetComponent<NetworkObject>().ChangeOwnership(OwnerClientId);

                    if (!avatar.HasInventorySpace(PlayerAvatar.Slot.PRIMARY)) {
                        avatar.DropItem(PlayerAvatar.Slot.PRIMARY);
                    }
                    if (!avatar.HasInventorySpace(PlayerAvatar.Slot.SECONDARY)) {
                        avatar.DropItem(PlayerAvatar.Slot.SECONDARY);
                    }

                    // destroy
                    po.ChangeOwnership(id);
                    avatar.GetComponent<NetworkObject>().ChangeOwnership(id);

                    Debug.Log("Disconnect: " + player.PlayerName);
                }
            };
    }

    void FixedUpdate() {
        Shader.SetGlobalInt("_StripeOffset", ((int) (Time.fixedTime * 16)) % 64);
        
    }

    public void RegisterPlayer(PersistentPlayer player, bool isLocal) {
        m_Players.Add(player);
        Debug.Log("Registered Player '" + player.PlayerName + "'");
        if (isLocal) {
            player.gameObject.transform.position = transform.position;
            player.gameObject.transform.rotation = transform.rotation;
            m_LocalPlayer = player;
            m_LocalPlayer.PlayerName = LocalPlayerName;
        }
    }

    public void StartShip(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut) {

        m_InGame = true;
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
