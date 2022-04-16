using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PersistentPlayer : NetworkBehaviour {

    private NetworkVariable<FixedString32Bytes> playerName
            = new NetworkVariable<FixedString32Bytes>();
    public string PlayerName {
        get {
            return playerName.Value.ToString();
        }
        set {
            if (IsOwner) {
                SetNameServerRpc(value);
            }
            // TODO notify server
        }
    }

    [SerializeField]
    private GameObject avatarPrefab;

    private PlayerAvatar avatar;
    public PlayerAvatar Avatar {
        get {
            return avatar;
        }
    }

    void Start() {
        PlayerManager.Instance.RegisterPlayer(this, IsOwner);
        PlayerName = SystemInfo.deviceName;
    }

    public void SpawnAvatar() {
        avatar = GameObject.Instantiate(avatarPrefab).GetComponent<PlayerAvatar>();
        avatar.playerName = PlayerName;
    }

    // Update is called once per frame
    void Update() {
        
    }

    [ServerRpc]
    public void SetNameServerRpc(string name) {
        playerName.Value = name;
    }
}
