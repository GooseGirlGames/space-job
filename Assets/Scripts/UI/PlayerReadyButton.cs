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

    protected override void Interaction(){
        m_LocalPlayerInteracting = !m_LocalPlayerInteracting;
        SetServerRpc(m_LocalPlayerInteracting);
        SetPlayerConditions(m_LocalPlayerInteracting); 
        SetReadyConditions(m_LocalPlayerInteracting);

    }
    void SetPlayerConditions(bool on){
        PlayerManager.Instance.LocalPlayer.Avatar.ready.Value = on;   
        
        /* if(readyCouter == PlayerManager.Instance.Players.Count)  NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single); */

    }
    void SetReadyConditions(bool ready){
        foreach (var player in FindObjectsOfType<PersistentPlayer>()) {
            if(IsOwner){
                if (PlayerManager.Instance.LocalPlayer.Avatar.ready.Value) {
                    PlayerManager.Instance.PlayersReady++;
                }
            }
            
        } 
    }

    public override void Update() {
        base.Update();
        Debug.Log("ready counter: " + PlayerManager.Instance.PlayersReady + " Player Counter: " + PlayerManager.Instance.Players.Count);
        if(PlayerManager.Instance.PlayersReady == PlayerManager.Instance.Players.Count){
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
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

