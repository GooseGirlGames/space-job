using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : Interactable<bool>{
    public Transform TeleportPoint;
    public override void OnStateChange(bool previous, bool current){}
    protected override void Interaction(){
        //Debug.Log("Chair interaction triggerd");
        PlayerManager.Instance.LocalPlayer.Avatar.SetPlayerPosServerRpc(new PlayerPos(TeleportPoint.position, TeleportPoint.rotation));
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer){
            m_State.Value = true;
        }
    }
}
