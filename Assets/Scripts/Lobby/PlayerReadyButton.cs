using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerReadyButton : Interactable<bool> {

    PlayerAvatar player;
    private bool m_LocalPlayerInteracting = false;
    //int readyCouter = 0;
    public NetworkVariable<int> PlayersReady =
        new NetworkVariable<int>(0);    
        
    [ServerRpc(RequireOwnership = false)]
    public void IncReadyServerRpc(){
        PlayersReady.Value = PlayersReady.Value + 1;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ResReadyServerRpc(){
        PlayersReady.Value = 0;
    }

    protected override void Interaction(){
        Debug.Log("hey" + PlayersReady.Value);
        m_LocalPlayerInteracting = !m_LocalPlayerInteracting;
        SetServerRpc(m_LocalPlayerInteracting);
        SetPlayerConditions(m_LocalPlayerInteracting);
        IncReadyServerRpc();  
        Debug.Log("hey" + PlayersReady.Value);
        /* SetReadyConditions(m_LocalPlayerInteracting); */

    }
    void SetPlayerConditions(bool on){
        PlayerManager.Instance.LocalPlayer.Avatar.ready.Value = !PlayerManager.Instance.LocalPlayer.Avatar.ready.Value;  
        
        
        /* if(readyCouter == PlayerManager.Instance.Players.Count)  NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single); */

    }
    void SetReadyConditions(bool ready){
/*         foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if(IsOwner){
                if (PlayerManager.Instance.LocalPlayer.Avatar.ready.Value) { */
        /* PlayerManager.Instance.PlayersReady++; */
      /*           }
            }
            
        } */ 
    }

    public override void Update() {
        base.Update();
        if(PlayersReady.Value == PlayerManager.Instance.Players.Count){
            if(IsServer) {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= PlayerManager.Instance.StartShip;
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += PlayerManager.Instance.MovePlayersToSpawns;
                NetworkManager.Singleton.SceneManager.LoadScene("ShipScene", LoadSceneMode.Single);
            }
        }
    }
    public override void OnStateChange(bool previous, bool current){
        //SetPlayerConditions(current);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetServerRpc(bool value){
        m_State.Value = value;
    }

}

