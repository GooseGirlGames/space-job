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
            Debug.Log("Name changed");
            if (IsOwner) {
                SetNameServerRpc(value);
            }
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
    }

    public void SpawnAvatar() {
        var owner = OwnerClientId;

        avatar = GameObject.Instantiate(avatarPrefab).GetComponent<PlayerAvatar>();
        avatar.GetComponent<NetworkObject>().Spawn();
        avatar.GetComponent<NetworkObject>().ChangeOwnership(owner);
    }

    // Update is called once per frame
    void Update() {
        
    }

    [ServerRpc]
    public void SetNameServerRpc(string name) {
        Debug.Log("SetNameServerRpc " + name);
        playerName.Value = name;
    }
}
